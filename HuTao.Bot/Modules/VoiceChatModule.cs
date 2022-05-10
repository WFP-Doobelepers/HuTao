using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Bot.Modules;

[Name("Voice")]
[Group("voice")]
[Summary("Commands to manage voice chats.")]
public class VoiceChatModule : ModuleBase<SocketCommandContext>
{
    private readonly HuTaoContext _db;

    public VoiceChatModule(HuTaoContext db) { _db = db; }

    [Command("ban")]
    [Summary("Ban someone from your current VC.")]
    public async Task BanAsync(IGuildUser user)
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id || user.Id == Context.User.Id)
            return;

        if (user.VoiceChannel?.Id == voiceChat.VoiceChannelId)
        {
            await user.VoiceChannel.AddPermissionOverwriteAsync(user,
                OverwritePermissions.DenyAll(user.VoiceChannel));
            await user.ModifyAsync(u => u.Channel = null);

            var textChannel = (IGuildChannel) Context.Channel;
            await textChannel.AddPermissionOverwriteAsync(user,
                OverwritePermissions.DenyAll(textChannel));

            var embed = new EmbedBuilder()
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .WithDescription("User has been banned from the channel.")
                .WithColor(Color.DarkRed);

            await ReplyAsync(embed: embed.Build());
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

        if (voiceChat.UserId == Context.User.Id)
        {
            await ReplyAsync("You already are the owner of the VC.");
            return;
        }

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        if (voiceChannel.Users.Any(u => u.Id == voiceChat.UserId))
        {
            await ReplyAsync("You cannot claim this VC.");
            return;
        }

        var owner = Context.Guild.GetUser(voiceChat.UserId);
        await voiceChannel.RemovePermissionOverwriteAsync(owner);
        await voiceChannel.AddPermissionOverwriteAsync(Context.User,
            new OverwritePermissions(manageChannel: PermValue.Allow, muteMembers: PermValue.Allow));

        voiceChat.UserId = Context.User.Id;
        await _db.SaveChangesAsync();

        await ReplyAsync("VC successfully claimed.");
    }

    [Command("clean")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    [Summary("Clean up unused Voice Chats.")]
    public async Task CleanAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        if (guild.VoiceChatRules is null)
            return;

        var voiceChats = guild.VoiceChatRules.VoiceChats.ToList();
        var empty = voiceChats.ToAsyncEnumerable()
            .Select(rules => new
            {
                VoiceChannel = Context.Guild.GetVoiceChannel(rules.VoiceChannelId),
                VoiceChat    = Context.Guild.GetTextChannel(rules.TextChannelId),
                Rules        = rules
            })
            .Where(v => v.VoiceChannel?.Users.All(u => u.IsBot || u.IsWebhook) ?? true);

        await foreach (var link in empty)
        {
            _db.Remove(link.Rules);
            _ = link.VoiceChannel?.DeleteAsync();
            _ = link.VoiceChat?.DeleteAsync();
        }

        await _db.SaveChangesAsync();
        await ReplyAsync($"Cleaned {Format.Bold(voiceChats.Count + " channel(s)")}.");
    }

    [Command("hide")]
    [Summary("Hide the VC from everyone. This denies view permission for everyone.")]
    public async Task HideAsync()
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
            new OverwritePermissions(viewChannel: PermValue.Deny));

        await ReplyAsync("VC hidden.");
    }

    [Command("kick")]
    [Summary("Kick someone from your VC.")]
    public async Task KickAsync(IGuildUser user)
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id || user.Id == Context.User.Id)
            return;

        if (user.VoiceChannel?.Id == voiceChat.VoiceChannelId)
        {
            await user.ModifyAsync(u => u.Channel = null);
            var embed = new EmbedBuilder()
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .WithDescription("User has been kicked from the channel.")
                .WithColor(Color.LightOrange);

            await ReplyAsync(embed: embed.Build());
        }
    }

    [Command("limit")]
    [Summary("Add a user limit to your VC.")]
    public async Task LimitAsync(uint? limit = null)
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await channel.ModifyAsync(c => c.UserLimit = new Optional<int?>((int?) limit));

        if (limit is null)
            await ReplyAsync("Voice user limit reset.");
        else
            await ReplyAsync($"User limit set to {Format.Bold(limit + " user(s)")}.");
    }

    [Command("lock")]
    [Summary("Lock the VC. This makes it so no one else can join." +
        " Leaving won't give you permission back unless you're the owner.")]
    public async Task LockAsync()
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var overwrite = new OverwritePermissions(connect: PermValue.Deny);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);

        await ReplyAsync("Voice chat locked successfully. " +
            $"Use {Format.Code(HuTaoConfig.Configuration.Prefix + "voice unlock")} to unlock.");
    }

    [Command("owner")]
    [Summary("Show the current owner of the VC.")]
    public async Task OwnerAsync()
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null)
            return;

        var owner = Context.Guild.GetUser(voiceChat.UserId);
        await ReplyAsync($"The current owner is {Format.Bold(owner.GetFullUsername())}.");
    }

    [Command("reveal")]
    [Summary("Reveals the VC from everyone. This sets the inherit permission on the everyone role.")]
    public async Task RevealAsync()
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var overwrite = new OverwritePermissions(viewChannel: PermValue.Inherit);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        await ReplyAsync("Voice chat revealed.");
    }

    [Command("transfer")]
    [Summary("Transfer ownership to someone else.")]
    public async Task TransferAsync(IGuildUser user, IVoiceChannel? channel)
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => channel != null
                ? vc.VoiceChannelId == channel.Id
                : vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || user.IsBot || user.IsWebhook)
            return;

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var canManage = Context.User is IGuildUser current && current.GetPermissions(voiceChannel).ManageChannel;
        if (voiceChat.Owner.Id != Context.User.Id && !canManage)
        {
            await ReplyAsync("You are not the owner of the VC.");
            return;
        }

        if (voiceChat.Owner.Id == user.Id)
        {
            await ReplyAsync("You already are the owner of the VC.");
            return;
        }

        await voiceChannel.RemovePermissionOverwriteAsync(Context.User);
        await voiceChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
            manageChannel: PermValue.Allow,
            muteMembers: PermValue.Allow));

        voiceChat.UserId = user.Id;
        await _db.SaveChangesAsync();

        await ReplyAsync(
            "Voice chat ownership successfully transferred " +
            $"to {Format.Bold(user.GetFullUsername())}.");
    }

    [Command("unban")]
    [Summary("Unban someone from your current VC.")]
    public async Task UnbanAsync(IGuildUser user)
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id || user.Id == Context.User.Id)
            return;

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await voiceChannel.RemovePermissionOverwriteAsync(user);

        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithDescription("User has been unbanned from the channel.")
            .WithColor(Color.Blue);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("unlock")]
    [Summary("Unlocks the VC.")]
    public async Task UnlockAsync()
    {
        var voiceChat = await _db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var everyone = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
        var overwrite = everyone?.Modify(connect: PermValue.Inherit)
            ?? new OverwritePermissions(connect: PermValue.Inherit);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        await ReplyAsync("Voice chat unlocked successfully.");
    }
}