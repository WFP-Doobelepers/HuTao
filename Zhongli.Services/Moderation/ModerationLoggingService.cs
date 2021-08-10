using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public class ModerationLoggingService
    {
        private readonly ZhongliContext _db;

        public ModerationLoggingService(ZhongliContext db) { _db = db; }

        public static string GetMessage(ReprimandAction action, IUser user)
        {
            return action switch
            {
                Ban        => $"{user} was banned.",
                Censored c => $"{user} was censored. Message: {c.CensoredMessage()}",
                Kick       => $"{user} was kicked.",
                Mute m     => $"{user} was muted for {GetLength(m)}.",
                Note       => $"{user} was given a note.",
                Notice     => $"{user} was given a notice.",
                Warning w  => $"{user} was warned {w.Amount} times.",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            static string GetLength(ILength mute)
                => mute.Length?.Humanize(5,
                    minUnit: TimeUnit.Second,
                    maxUnit: TimeUnit.Year) ?? "indefinitely";
        }

        public static string GetTitle(ReprimandAction action)
        {
            var title = action switch
            {
                Ban      => nameof(Ban),
                Censored => nameof(Censored),
                Kick     => nameof(Kick),
                Mute     => nameof(Mute),
                Note     => nameof(Note),
                Notice   => nameof(Notice),
                Warning  => nameof(Warning),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            return $"{title.Humanize()}: {action.Id}";
        }

        public async Task PublishReprimandAsync(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
        {
            var embed = await UpdatedEmbedAsync(reprimand, details, cancellationToken);
            await HandleReprimandAsync(embed, reprimand, details.User, details.Moderator, cancellationToken);
        }

        public async Task PublishReprimandAsync(ReprimandResult reprimand, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = await CreateEmbedAsync(reprimand, details, cancellationToken);
            await HandleReprimandAsync(embed, reprimand, details.User, details.Moderator, cancellationToken);
        }

        public async Task<EmbedBuilder> CreateEmbedAsync(ReprimandResult result, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User);

            await AddPrimaryAsync(embed, details, result.Primary, cancellationToken);
            foreach (var secondary in result.Secondary)
            {
                await AddSecondaryAsync(embed, details, secondary, cancellationToken);
            }

            return embed;
        }

        public async Task<EmbedBuilder> UpdatedEmbedAsync(ReprimandAction reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User)
                .WithTitle($"{reprimand.Status.Humanize()} {GetTitle(reprimand)}")
                .WithColor(Color.Purple);

            AddReason(embed, reprimand.ModifiedAction);
            await AddReprimandDetailsAsync(embed, reprimand, cancellationToken);

            return embed;
        }

        private static bool IsIncluded(ReprimandAction action, ReprimandNoticeType type)
        {
            if (type == ReprimandNoticeType.All)
                return true;

            return action switch
            {
                Ban      => type.HasFlag(ReprimandNoticeType.Ban),
                Censored => type.HasFlag(ReprimandNoticeType.Censor),
                Kick     => type.HasFlag(ReprimandNoticeType.Kick),
                Mute     => type.HasFlag(ReprimandNoticeType.Mute),
                Notice   => type.HasFlag(ReprimandNoticeType.Notice),
                Warning  => type.HasFlag(ReprimandNoticeType.Warning),
                _        => false
            };
        }

        private static Color GetColor(ReprimandAction action)
        {
            return action switch
            {
                Ban      => Color.Red,
                Censored => Color.Blue,
                Kick     => Color.Red,
                Mute     => Color.Orange,
                Note     => Color.Blue,
                Notice   => Color.Gold,
                Warning  => Color.Gold,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };
        }

        private static EmbedBuilder CreateReprimandEmbed(IUser user) => new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail, ushort.MaxValue)
            .WithCurrentTimestamp();

        private async Task AddPrimaryAsync(EmbedBuilder embed, ReprimandDetails details, ReprimandAction? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                embed
                    .WithTitle(GetTitle(reprimand))
                    .WithColor(GetColor(reprimand))
                    .WithDescription(GetMessage(reprimand, details.User));

                AddReason(embed, reprimand.Action);
                await AddReprimandDetailsAsync(embed, reprimand, cancellationToken);
            }
        }

        private async Task AddReprimandDetailsAsync(EmbedBuilder embed, ReprimandAction reprimand,
            CancellationToken cancellationToken)
            => embed
                .AddField("Total", await GetTotalAsync(reprimand, cancellationToken), true)
                .AddField("Source", reprimand.Source.Humanize(), true);

        private async Task AddSecondaryAsync(EmbedBuilder embed, ReprimandDetails details, ReprimandAction? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                var total = await GetTotalAsync(reprimand, cancellationToken);
                embed
                    .WithColor(GetColor(reprimand))
                    .AddField($"{GetTitle(reprimand)} [{total}]", $"{GetMessage(reprimand, details.User)}");
            }
        }

        private async Task HandleReprimandAsync(
            EmbedBuilder embed,
            ReprimandResult result,
            IUser user, IGuildUser moderator,
            CancellationToken cancellationToken)
        {
            var reprimand = result.Primary;
            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.LoggingRules.Options;
            if (!options.HasFlag(LoggingOptions.Verbose)
                && reprimand.Status is ReprimandStatus.Added
                && reprimand.Source is ModerationSource.Notice or ModerationSource.Warning)
                return;

            var channelId = guild.LoggingRules.ModerationChannelId;
            if (channelId is null)
                return;

            AddReprimandAuthor(moderator, false, embed);
            var channel = await moderator.Guild.GetTextChannelAsync(channelId.Value);
            _ = channel.SendMessageAsync(embed: embed.Build());

            if (options.HasFlag(LoggingOptions.NotifyUser)
                && reprimand is not Note
                && IsIncluded(reprimand, guild.LoggingRules.NotifyReprimands)
                && reprimand.Status
                    is ReprimandStatus.Added
                    or ReprimandStatus.Expired
                    or ReprimandStatus.Updated)
            {
                AddReprimandAuthor(moderator, options.HasFlag(LoggingOptions.Anonymous), embed);
                if (IsIncluded(reprimand, guild.LoggingRules.ShowAppealOnReprimands))
                {
                    var appealMessage = guild.LoggingRules.ReprimandAppealMessage;
                    if (!string.IsNullOrWhiteSpace(appealMessage))
                        embed.AddField("Appeal", appealMessage);
                }

                var dm = await user.GetOrCreateDMChannelAsync();
                _ = dm?.SendMessageAsync(embed: embed.Build());
            }
        }

        private async ValueTask<uint> GetTotalAsync(ReprimandAction action, CancellationToken cancellationToken)
        {
            var user = await action.GetUserAsync(_db, cancellationToken);

            return action switch
            {
                Ban      => user.HistoryCount<Ban>(),
                Censored => user.HistoryCount<Censored>(),
                Kick     => user.HistoryCount<Kick>(),
                Mute     => user.HistoryCount<Mute>(),
                Note     => user.HistoryCount<Note>(),
                Notice   => user.HistoryCount<Notice>(),
                Warning  => user.WarningCount(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };
        }

        private static void AddReason(EmbedBuilder embed, ModerationAction? action)
        {
            if (!string.IsNullOrWhiteSpace(action?.Reason))
                embed.AddField("Reason", action.Reason);
        }

        private static void AddReprimandAuthor(IGuildUser moderator, bool isAnonymous, EmbedBuilder embed)
        {
            if (isAnonymous)
                embed.WithGuildAsAuthor(moderator.Guild, AuthorOptions.UseFooter | AuthorOptions.Requested);
            else
                embed.WithUserAsAuthor(moderator, AuthorOptions.UseFooter | AuthorOptions.Requested);
        }
    }
}