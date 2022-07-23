using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using static HuTao.Data.Models.Moderation.Auto.Configurations.DuplicateConfiguration.DuplicateType;

namespace HuTao.Services.Moderation;

public class AutoModerationBehavior :
    INotificationHandler<MessageReceivedNotification>,
    INotificationHandler<MessageUpdatedNotification>
{
    private const StringSplitOptions SplitOptions
        = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);
    private static readonly char[] Separators = { ' ', '\r', '\n' };
    private readonly DiscordSocketClient _client;
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;
    private readonly ModerationService _moderation;

    public AutoModerationBehavior(
        DiscordSocketClient client, HuTaoContext db,
        IMemoryCache cache, ModerationService moderation)
    {
        _client     = client;
        _db         = db;
        _cache      = cache;
        _moderation = moderation;
    }

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.Message, cancellationToken);

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
        => ProcessMessage(notification.NewMessage, cancellationToken);

    private ConcurrentDictionary<ulong, IUserMessage> MessageDictionary(IGuildUser user)
        => _cache.GetOrCreate($"{nameof(MessageDictionary)}.{user.Guild.Id}.{user.Id}", e =>
        {
            e.SlidingExpiration = CacheExpiration;
            return new ConcurrentDictionary<ulong, IUserMessage>();
        });

    private ConcurrentQueue<ulong> MessageQueue(IGuildUser user)
        => _cache.GetOrCreate($"{nameof(MessageQueue)}.{user.Guild.Id}.{user.Id}", e =>
        {
            e.SlidingExpiration = CacheExpiration;
            return new ConcurrentQueue<ulong>();
        });

    private async Task ProcessMessage(IMessage source, CancellationToken cancellationToken = default)
    {
        if (source is not IUserMessage
            {
                Author: IGuildUser user,
                Author.IsBot: false,
                Channel: SocketTextChannel channel
            } message) return;

        if (IsUserCooldown()) return;

        var rules = await _db.Guilds.GetRulesAsync(user.Guild, _cache, cancellationToken);
        if (rules is null) return;

        if (rules.Exclusions.OfType<CriterionExclusion>().Any(c => c.Criterion.Judge(channel, user))
            && channel.Id is not 891703978566496306) return;

        AddMessage(user, message);

        foreach (var config in Rules<MessageConfiguration>())
        {
            if (await TryReprimand(config, _ => 1))
                return;
        }

        foreach (var config in Rules<DuplicateConfiguration>())
        {
            var messages = GetMessages(config);
            var results = messages.Select(m =>
            {
                return config.Type switch
                {
                    Message   => new DuplicateResult(m, config.IsDuplicate(message, m), 1),
                    Word      => m.Memoized(config.Type, _ => WordDuplicates(), $"{config.Id}"),
                    Character => m.Memoized(config.Type, _ => CharacterDuplicates(), $"{config.Id}"),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(config.Type), config.Type, "Unknown duplicate type.")
                };

                DuplicateResult WordDuplicates()
                {
                    var words = m.Content.Split(Separators, SplitOptions);
                    var duplicates = words.Distinct().Max(word => words.Count(w => config.IsDuplicate(word, w)));
                    return new DuplicateResult(m, duplicates, words.Length);
                }

                DuplicateResult CharacterDuplicates()
                {
                    var chars = m.Content.Replace(" ", string.Empty).ToCharArray();
                    var duplicates = chars.Distinct().Max(character => chars.Count(c => c == character));
                    return new DuplicateResult(m, duplicates, chars.Length);
                }
            }).ToList();

            var percentage = (double) results.Sum(r => r.Count) / results.Sum(r => r.Total);
            if (double.IsNaN(percentage) || percentage < config.Percentage)
                continue;

            if (await Reprimand(config, results))
                return;
        }

        foreach (var config in Rules<AttachmentConfiguration>())
        {
            if (await TryReprimand(config, m => m.Attachments.Count))
                return;
        }

        foreach (var config in Rules<EmojiConfiguration>())
        {
            if (await TryReprimand(config, m =>
                {
                    var emoji = RegexUtilities.Emoji.Matches(m.Content).Select(match => match.Value);
                    var emote = RegexUtilities.Emote.Matches(m.Content).Select(match => match.Value);

                    var exclusions = rules.EmojiExclusions(config);
                    return emoji.Concat(emote).Count(e => exclusions.None(e));
                })) return;
        }

        foreach (var config in Rules<InviteConfiguration>())
        {
            if (await TryReprimandAwait(config, async m =>
                {
                    var exclusions = rules.InviteExclusions(config);
                    return await RegexUtilities.Invite.Matches(m.Content).ToAsyncEnumerable()
                        .SelectAwait(async i => await _cache.GetInviteAsync(_client, i.Groups["code"].Value))
                        .Where(i => i is not null && exclusions.All(e => !e.Judge(i)))
                        .CountAsync(cancellationToken);
                })) return;
        }

        foreach (var config in Rules<LinkConfiguration>())
        {
            if (await TryReprimand(config, m =>
                {
                    var exclusions = rules.LinkExclusions(config);
                    return RegexUtilities.Link.Matches(m.Content)
                        .Where(u => Uri.IsWellFormedUriString(u.Value, UriKind.Absolute))
                        .Select(u => new Uri(u.Value))
                        .Count(u => exclusions.None(u));
                })) return;
        }

        foreach (var config in Rules<MentionConfiguration>())
        {
            if (await TryReprimand(config, m =>
                {
                    var userExclusions = rules.UserExclusions(config);
                    var roleExclusions = rules.RoleExclusions(config);

                    if (config.CountInvalid)
                    {
                        var mentions = RegexUtilities.Mention.Matches(m.Content).Where(mention
                            => ulong.TryParse(mention.Groups["id"].Value, out var id)
                            && userExclusions.None(id) && roleExclusions.None(id));

                        return config.CountDuplicate
                            ? mentions.DistinctBy(mention => mention.Groups["id"].Value).Count()
                            : mentions.Count();
                    }

                    var users = m.MentionedUserIds.Where(u => userExclusions.None(u)).Sum(Duplicates);
                    var roles = m.MentionedRoleIds.Where(r => roleExclusions.None(r))
                        .Select(r => channel.Guild.GetRole(r)).Where(r => r is not null)
                        .Sum(r => config.CountRoleMembers ? r.Members.Count() : Duplicates(r.Id));

                    return users + roles;

                    int Duplicates(ulong id) => config.CountDuplicate ? Mentions(id).Count : 1;

                    MatchCollection Mentions(ulong id) => RegexUtilities.Mentions(id).Matches(m.Content);
                })) return;
        }

        foreach (var config in Rules<ReplyConfiguration>())
        {
            if (await TryReprimand(config,
                m =>
                {
                    var userExclusions = rules.UserExclusions(config);
                    var roleExclusions = rules.RoleExclusions(config);

                    return message.ReferencedMessage is IMessage { Author: IGuildUser author, Author.IsBot: false }
                        && userExclusions.None(author.Id) && roleExclusions.None(author.RoleIds)
                        && m.MentionedUserIds.Contains(author.Id);
                })) return;
        }

        foreach (var config in Rules<NewLineConfiguration>())
        {
            if (await TryReprimand(config, m => config.BlankOnly
                ? RegexUtilities.BlankLine.Matches(m.Content).Count
                : RegexUtilities.NewLine.Matches(m.Content).Count))
                return;
        }

        IEnumerable<IUserMessage> GetMessages(AutoConfiguration config) => MessageDictionary(user).Values
            .Where(m => m.Content.Length >= config.MinimumLength)
            .Where(m => DateTimeOffset.Now - (m.EditedTimestamp ?? m.Timestamp) < config.Length)
            .Where(m => config.Global || m.Channel.Id == channel.Id);

        IEnumerable<T> Rules<T>() where T : AutoConfiguration
            => rules.Triggers.OfType<T>().ToList().Where(IsIncluded).Where(c => !IsConfigCooldown(c));

        bool IsIncluded(AutoConfiguration config) => !config.Exclusions
            .OfType<CriterionExclusion>()
            .Any(e => e.Criterion.Judge(channel, user));

        Task<bool> TryReprimand(AutoConfiguration config, Func<IUserMessage, Count> counter)
            => Reprimand(config, GetMessages(config).Select(m
                => new Result(m, m.Memoized(m, counter, $"{config.Id}"))).ToList());

        async Task<bool> TryReprimandAwait(AutoConfiguration config, Func<IUserMessage, Task<Count>> counter)
            => await Reprimand(config, await GetMessages(config).ToAsyncEnumerable()
                .SelectAwait(async m => new Result(m, await m.Memoized(m, counter, $"{config.Id}")))
                .ToListAsync(cancellationToken));

        async Task<bool> Reprimand<T>(AutoConfiguration config, ICollection<T> messages) where T : Result
        {
            var semaphore = _cache.GetOrCreate($"{nameof(Reprimand)}.{user.Guild.Id}", e =>
            {
                e.SlidingExpiration = CacheExpiration;
                return new SemaphoreSlim(1, 1);
            });

            try
            {
                await semaphore.WaitAsync(cancellationToken);
                if (IsUserCooldown() || IsConfigCooldown(config))
                    return false;

                if (!config.IsTriggered((uint) messages.Sum(m => m.Count)))
                    return false;

                var cooldown = config.Category?.AutoReprimandCooldown ?? rules.AutoReprimandCooldown;
                if (cooldown is not null && cooldown > TimeSpan.Zero)
                    _cache.Set($"{nameof(IsUserCooldown)}.{user.Id}", DateTimeOffset.Now, cooldown.Value);
                if (config.Cooldown is not null && config.Cooldown > TimeSpan.Zero)
                    _cache.Set($"{nameof(IsUserCooldown)}.{user.Id}", DateTimeOffset.Now, config.Cooldown.Value);

                var delete = messages.Where(m => m.Count > 0).Select(m => m.Message).ToList();
                var length = config.Category?.FilteredExpiryLength ?? rules.FilteredExpiryLength;
                var details = GetDetails(config, (uint) messages.Sum(m => m.Count));

                return await _moderation.AutoReprimandAsync(delete, length, details, cancellationToken) is not null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        bool IsUserCooldown() => _cache.TryGetValue($"{nameof(IsUserCooldown)}.{user.Id}", out _);

        bool IsConfigCooldown(Trigger config) => _cache.TryGetValue($"{config.Id}.{user.Id}", out _);

        ReprimandDetails GetDetails(AutoConfiguration config, uint count) => new(
            message.Author, channel.Guild.CurrentUser, Trigger: config, Category: config.Category,
            Reason: config.Reason ?? $"[{config.GetTitle()} Limit of {config.Amount} Triggered] at {count}");
    }

    private void AddMessage(IGuildUser user, IUserMessage message)
    {
        var queue = MessageQueue(user);
        var dict = MessageDictionary(user);

        queue.Enqueue(message.Id);
        while (queue.Count > 100 && queue.TryDequeue(out var id))
        {
            dict.TryRemove(id, out _);
        }

        dict.AddOrUpdate(message.Id, message, (_, _) => message);
    }

    private record DuplicateResult(IUserMessage Message, Count Count, Count Total) : Result(Message, Count);

    private record Result(IUserMessage Message, Count Count);

    private record Count(uint Amount)
    {
        public static implicit operator Count(bool success) => new(success ? 1 : 0);

        public static implicit operator Count(uint count) => new(count);

        public static implicit operator Count(int count) => new((uint) Math.Max(count, 0));

        public static implicit operator uint(Count count) => count.Amount;
    }
}