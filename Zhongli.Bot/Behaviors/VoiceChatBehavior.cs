using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.VoiceChat;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Bot.Behaviors
{
    public class VoiceChatBehavior : INotificationHandler<UserVoiceStateNotification>
    {
        private readonly ZhongliContext _db;

        public VoiceChatBehavior(ZhongliContext db) { _db = db; }

        public async Task Handle(UserVoiceStateNotification notification, CancellationToken cancellationToken)
        {
            var oldChannel = notification.Old.VoiceChannel;
            var newChannel = notification.New.VoiceChannel;

            if (notification.User is not SocketGuildUser user)
                return;

            var guild = user.Guild;
            var rules = await _db.Set<VoiceChatRules>().AsQueryable()
                .FirstOrDefaultAsync(r => r.GuildId == guild.Id, cancellationToken);

            if (newChannel?.Id == rules.HubVoiceChannelId)
            {
                var ownerOf = rules.VoiceChats.FirstOrDefault(v => v.OwnerId == notification.User.Id);
                if (ownerOf is not null)
                {
                    var voiceChannel = guild.GetVoiceChannel(ownerOf.VoiceChannelId);
                    await user.ModifyAsync(u => u.Channel = voiceChannel);
                }
                else
                {
                    var voiceChannels = rules.VoiceChats.Select(v =>
                        guild.GetVoiceChannel(v.VoiceChannelId));

                    var ruleNumbers = voiceChannels
                        .Select(v => Regex.Match(v.Name, @"^VC (?<i>[0-9]+)$"))
                        .Where(m => m.Success)
                        .Select(m => uint.Parse(m.Groups["i"].Value));

                    var maxId = ruleNumbers.DefaultIfEmpty<uint>(0).Max() + 1;

                    var voiceChannel = await guild.CreateVoiceChannelAsync($"VC {maxId}",
                        c => c.CategoryId = rules.VoiceChannelCategoryId);
                    await voiceChannel.AddPermissionOverwriteAsync(user,
                        new OverwritePermissions(manageChannel: PermValue.Allow));

                    var textChannel = await guild.CreateTextChannelAsync($"vc-{maxId}",
                        c => c.CategoryId = rules.VoiceChatCategoryId);
                    await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                        new OverwritePermissions(viewChannel: PermValue.Deny));

                    var voiceChat = new VoiceChatLink
                    {
                        OwnerId        = user.Id,
                        GuildId        = guild.Id,
                        TextChannelId  = textChannel.Id,
                        VoiceChannelId = voiceChannel.Id
                    };

                    rules.VoiceChats.Add(voiceChat);

                    _db.Update(rules);
                    await _db.SaveChangesAsync(cancellationToken);

                    await user.ModifyAsync(u => u.Channel = voiceChannel);
                }
            }
            else if (oldChannel is not null && newChannel is null)
            {
                var users = oldChannel.Users.Where(u => !u.IsBot);
                if (!users.Any())
                {
                    var voiceChat = rules.VoiceChats.FirstOrDefault(v => v.VoiceChannelId == oldChannel.Id);
                    if (voiceChat is not null && rules.PurgeEmpty)
                    {
                        var voiceChannel = guild.GetVoiceChannel(voiceChat.VoiceChannelId);
                        var textChannel = guild.GetTextChannel(voiceChat.TextChannelId);

                        await voiceChannel.DeleteAsync();
                        await textChannel.DeleteAsync();

                        _db.Remove(voiceChat);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else if (newChannel is not null)
            {
                var voiceChat = rules.VoiceChats.FirstOrDefault(v => v.VoiceChannelId == newChannel.Id);
                if (voiceChat is not null)
                {
                    var textChannel = guild.GetTextChannel(voiceChat.TextChannelId);
                    await textChannel.AddPermissionOverwriteAsync(user,
                        new OverwritePermissions(viewChannel: PermValue.Allow));
                }
            }
        }
    }
}