using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Bot.Behaviors;

public class VoiceChatBehavior : INotificationHandler<UserVoiceStateNotification>
{
    private static readonly Regex VcRegex = new(@"^VC([ ]|-)(?<i>[0-9]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HuTaoContext _db;
    private readonly ICommandHelpService _commandHelp;

    public VoiceChatBehavior(ICommandHelpService commandHelp, HuTaoContext db)
    {
        _commandHelp = commandHelp;
        _db          = db;
    }

    private static ConcurrentDictionary<ulong, CancellationTokenSource> PurgeTasks { get; } = new();

    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier")]
    public async Task Handle(UserVoiceStateNotification notification, CancellationToken cancellationToken)
    {
        if (notification.User.IsBot || notification.User.IsWebhook || notification.User is not SocketGuildUser user)
            return;

        await _db.Users.TrackUserAsync(user, cancellationToken);

        var guild = user.Guild;
        var rules = await _db.Set<VoiceChatRules>().AsQueryable()
            .FirstOrDefaultAsync(r => r.GuildId == guild.Id, cancellationToken);

        if (rules is null)
            return;

        var oldChannel = notification.Old.VoiceChannel;
        var newChannel = notification.New.VoiceChannel;

        if (newChannel?.Id == rules.HubVoiceChannelId)
        {
            var ownerOf = rules.VoiceChats.FirstOrDefault(v => v.UserId == notification.User.Id);
            if (ownerOf is not null)
            {
                var voiceChannel = guild.GetVoiceChannel(ownerOf.VoiceChannelId);
                await user.ModifyAsync(u => u.Channel = voiceChannel);
            }
            else
            {
                var voiceCategory = rules.VoiceChannelCategoryId;
                var chatCategory = rules.VoiceChatCategoryId;

                var voiceChannelCategory = guild.GetCategoryChannel(voiceCategory);
                var voiceChatCategory = guild.GetCategoryChannel(chatCategory);

                var ruleNumbers = voiceChannelCategory.Channels.Concat(voiceChatCategory.Channels)
                    .Select(v => VcRegex.Match(v.Name))
                    .Where(m => m.Success && uint.TryParse(m.Groups["i"].Value, out _))
                    .Select(m => uint.Parse(m.Groups["i"].Value))
                    .Distinct().ToList();

                // To get the next available number, sort the list and then
                // get the index of the first element that does not match its index.
                // If there is nothing that match, then the next value must be the length of the list.
                var maxId = ruleNumbers.OrderBy(x => x).AsIndexable()
                    .Where(item => item.Index != item.Value)
                    .Select(item => item.Index)
                    .DefaultIfEmpty(ruleNumbers.Count)
                    .FirstOrDefault();

                var voice = await guild.CreateVoiceChannelAsync($"VC {maxId}", c => c.CategoryId = voiceCategory);
                var allow = new OverwritePermissions(manageChannel: PermValue.Allow);
                await voice.AddPermissionOverwriteAsync(user, allow);

                var chat = await guild.CreateTextChannelAsync($"vc-{maxId}", c => c.CategoryId = chatCategory);
                var deny = new OverwritePermissions(viewChannel: PermValue.Deny);
                await chat.AddPermissionOverwriteAsync(guild.EveryoneRole, deny);

                if (_commandHelp.TryGetEmbed("voice", HelpDataType.Module, out var paginated))
                {
                    var embed = await paginated.Build().GetOrLoadCurrentPageAsync();
                    var message = await chat.SendMessageAsync(embeds: embed.GetEmbedArray());

                    await message.PinAsync();
                }

                var voiceChat = new VoiceChatLink
                {
                    UserId         = user.Id,
                    GuildId        = guild.Id,
                    TextChannelId  = chat.Id,
                    VoiceChannelId = voice.Id
                };

                rules.VoiceChats.Add(voiceChat);

                _db.Update(rules);
                await _db.SaveChangesAsync(cancellationToken);

                await user.ModifyAsync(u => u.Channel = voice);
            }
        }

        if (newChannel is not null && newChannel.Id != oldChannel?.Id)
        {
            if (PurgeTasks.TryRemove(newChannel.Id, out var token))
                token.Cancel();

            var voiceChat = rules.VoiceChats.FirstOrDefault(v => v.VoiceChannelId == newChannel.Id);
            if (voiceChat is not null)
            {
                var textChannel = guild.GetTextChannel(voiceChat.TextChannelId);
                await textChannel.AddPermissionOverwriteAsync(user,
                    new OverwritePermissions(viewChannel: PermValue.Allow));

                if (rules.ShowJoinLeave)
                    await textChannel.SendMessageAsync($"{user.Mention} has joined the VC. You can chat in here.");
            }
        }

        if (oldChannel is not null && oldChannel.Id != newChannel?.Id)
        {
            var users = oldChannel.ConnectedUsers.Where(u => !u.IsBot);
            var voiceChat = rules.VoiceChats.FirstOrDefault(v => v.VoiceChannelId == oldChannel.Id);

            if (voiceChat is not null)
            {
                var textChannel = guild.GetTextChannel(voiceChat.TextChannelId);
                _ = textChannel?.RemovePermissionOverwriteAsync(user);

                if (rules.PurgeEmpty && !users.Any() && !PurgeTasks.ContainsKey(oldChannel.Id))
                {
                    var tokenSource = new CancellationTokenSource();

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(rules.DeletionDelay, tokenSource.Token);

                        var voiceChannel = guild.GetVoiceChannel(voiceChat.VoiceChannelId);
                        if (voiceChannel?.ConnectedUsers.Any() ?? false) return;

                        _ = voiceChannel?.DeleteAsync();
                        _ = textChannel?.DeleteAsync();

                        _db.Remove(voiceChat);
                        await _db.SaveChangesAsync(cancellationToken);
                    }, tokenSource.Token);

                    PurgeTasks.TryAdd(oldChannel.Id, tokenSource);
                }
            }
        }
    }
}