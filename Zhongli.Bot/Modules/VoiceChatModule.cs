using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.VoiceChat;

namespace Zhongli.Bot.Modules
{
    [Group("voice")]
    public class VoiceChatModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public VoiceChatModule(ZhongliContext db) { _db = db; }

        [Command("ban")]
        [Summary("Ban someone from your current VC.")]
        public async Task BanAsync(IGuildUser user)
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id || user.Id == Context.User.Id)
                return;

            if (user.VoiceChannel?.Id == voiceChat.VoiceChannelId)
            {
                await user.ModifyAsync(u => u.Channel = null);
                await user.VoiceChannel.AddPermissionOverwriteAsync(user,
                    OverwritePermissions.DenyAll(user.VoiceChannel));

                var textChannel = (IGuildChannel) Context.Channel;
                await textChannel.AddPermissionOverwriteAsync(user,
                    OverwritePermissions.DenyAll(textChannel));

                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("claim")]
        [Summary("Claim this VC as yours. Only works if there are no other people in the VC.")]
        public async Task ClaimAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null)
                return;

            if (voiceChat.OwnerId == Context.User.Id)
            {
                await ReplyAsync("You already are the owner of the VC.");
                return;
            }

            var voiceChannel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            var users = await voiceChannel.GetUsersAsync().FlattenAsync();
            if (users.Any(u => u.Id == voiceChat.OwnerId))
            {
                await ReplyAsync("You cannot claim this VC.");
                return;
            }

            var owner = await Context.Guild.GetUserAsync(voiceChat.OwnerId);
            await voiceChannel.RemovePermissionOverwriteAsync(owner);
            await voiceChannel.AddPermissionOverwriteAsync(Context.User,
                new OverwritePermissions(manageChannel: PermValue.Allow, muteMembers: PermValue.Allow));

            voiceChat.OwnerId = Context.User.Id;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("clean")]
        [Summary("Clean up unused Voice Chats.")]
        public async Task CleanAsync()
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);

            if (guild.VoiceChatRules is null)
                return;

            var voiceChats = guild.VoiceChatRules.VoiceChats.ToList();
            var empty = voiceChats.ToAsyncEnumerable()
                .SelectAwait(async v => new
                {
                    VoiceChannel = await Context.Guild.GetVoiceChannelAsync(v.VoiceChannelId),
                    VoiceChat    = await Context.Guild.GetTextChannelAsync(v.TextChannelId)
                })
                .WhereAwait(async v =>
                {
                    var users = await v.VoiceChannel.GetUsersAsync().FlattenAsync();
                    return users.All(u => u.IsBot || u.IsWebhook);
                });

            await foreach (var link in empty)
            {
                var voiceChat = voiceChats.FirstOrDefault(v => v.VoiceChannelId == link.VoiceChannel.Id);

                _db.Remove(voiceChat!);

                await link.VoiceChannel.DeleteAsync();
                await link.VoiceChat.DeleteAsync();
            }

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("hide")]
        [Summary("Hide the VC from everyone. This denies view permission for everyone.")]
        public async Task HideAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id)
                return;

            var channel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
                new OverwritePermissions(viewChannel: PermValue.Deny));

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("kick")]
        [Summary("Kick someone from your VC.")]
        public async Task KickAsync(IGuildUser user)
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id || user.Id == Context.User.Id)
                return;

            if (user.VoiceChannel?.Id == voiceChat.VoiceChannelId)
            {
                await user.ModifyAsync(u => u.Channel = null);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
        }

        [Command("limit")]
        [Summary("Add a user limit to your VC.")]
        public async Task LimitAsync(uint? limit = null)
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id)
                return;

            var channel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            await channel.ModifyAsync(c => c.UserLimit = new Optional<int?>((int?) limit));

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("lock")]
        [Summary("Lock the VC. This makes it so no one else can join." +
            " Leaving won't give you permission back unless you're the owner.")]
        public async Task LockAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id)
                return;

            var channel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            var everyone = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
            var overwrite = everyone?.Modify(connect: PermValue.Deny)
                ?? new OverwritePermissions(connect: PermValue.Deny);

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        }

        [Command("owner")]
        [Summary("Show the current owner of the VC.")]
        public async Task OwnerAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null)
                return;

            var owner = await Context.Guild.GetUserAsync(voiceChat.OwnerId);
            await ReplyAsync($"The current owner is {owner}.");
        }

        [Command("reveal")]
        [Summary("Reveals the VC from everyone. This sets the inherit permission on the everyone role.")]
        public async Task RevealAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id)
                return;

            var channel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            var everyone = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
            var overwrite = everyone?.Modify(viewChannel: PermValue.Inherit)
                ?? new OverwritePermissions(viewChannel: PermValue.Inherit);

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("transfer")]
        [Summary("Transfer ownership to someone else.")]
        public async Task TransferAsync(IGuildUser user)
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || user.IsBot || user.IsWebhook)
                return;

            if (voiceChat.OwnerId == user.Id)
            {
                await ReplyAsync("You already are the owner of the VC.");
                return;
            }

            var voiceChannel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);

            await voiceChannel.RemovePermissionOverwriteAsync(Context.User);
            await voiceChannel.AddPermissionOverwriteAsync(user,
                new OverwritePermissions(manageChannel: PermValue.Allow, muteMembers: PermValue.Allow));

            voiceChat.OwnerId = user.Id;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("unban")]
        [Summary("Unban someone from your current VC.")]
        public async Task UnbanAsync(IGuildUser user)
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id || user.Id == Context.User.Id)
                return;

            var voiceChannel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            await voiceChannel.RemovePermissionOverwriteAsync(user);

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("unlock")]
        [Summary("Unlocks the VC.")]
        public async Task UnlockAsync()
        {
            var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
                .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

            if (voiceChat is null || voiceChat.OwnerId != Context.User.Id)
                return;

            var channel = await Context.Guild.GetVoiceChannelAsync(voiceChat.VoiceChannelId);
            var everyone = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
            var overwrite = everyone?.Modify(connect: PermValue.Inherit)
                ?? new OverwritePermissions(connect: PermValue.Inherit);

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}