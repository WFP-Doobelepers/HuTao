using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.VoiceChat;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Bot.Modules;

[Name("Voice")]
[Group("voice")]
[Summary("Commands to manage voice chats.")]
public class VoiceChatModule(HuTaoContext db) : ModuleBase<SocketCommandContext>
{
    private const uint AccentColor = 0x9B59FF;

    [Command("ban")]
    [Summary("Ban someone from your current VC.")]
    public async Task BanAsync(IGuildUser user)
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
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

            await ReplyPanelAsync(
                "User banned",
                $"{user.Mention} has been banned from this voice chat.",
                Color.DarkRed.RawValue);
        }
    }

    [Command("claim")]
    [Summary("Claim this VC as yours. Only works if there are no other people in the VC.")]
    public async Task ClaimAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null)
            return;

        if (voiceChat.UserId == Context.User.Id)
        {
            await ReplyPanelAsync("Voice Chat", "You already are the owner of this voice chat.");
            return;
        }

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        if (voiceChannel.Users.Any(u => u.Id == voiceChat.UserId))
        {
            await ReplyPanelAsync("Voice Chat", "You cannot claim this voice chat right now.");
            return;
        }

        var owner = Context.Guild.GetUser(voiceChat.UserId);
        await voiceChannel.RemovePermissionOverwriteAsync(owner);
        await voiceChannel.AddPermissionOverwriteAsync(Context.User,
            new OverwritePermissions(manageChannel: PermValue.Allow, muteMembers: PermValue.Allow));

        voiceChat.UserId = Context.User.Id;
        await db.SaveChangesAsync();

        await ReplyPanelAsync("Voice Chat", "Voice chat successfully claimed.");
    }

    [Command("clean")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    [Summary("Clean up unused Voice Chats.")]
    public async Task CleanAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
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
            .Where(v => v.VoiceChannel?.ConnectedUsers.All(u => u.IsBot || u.IsWebhook) ?? true);

        await foreach (var link in empty)
        {
            db.Remove(link.Rules);
            _ = link.VoiceChannel?.DeleteAsync();
            _ = link.VoiceChat?.DeleteAsync();
        }

        await db.SaveChangesAsync();
        await ReplyPanelAsync("Voice Chat Cleanup", $"Cleaned {Format.Bold(voiceChats.Count + " channel(s)")}.", AccentColor);
    }

    [Command("hide")]
    [Summary("Hide the VC from everyone. This denies view permission for everyone.")]
    public async Task HideAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
            new OverwritePermissions(viewChannel: PermValue.Deny));

        await ReplyPanelAsync("Voice Chat", "Voice chat hidden.");
    }

    [Command("kick")]
    [Summary("Kick someone from your VC.")]
    public async Task KickAsync(IGuildUser user)
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id || user.Id == Context.User.Id)
            return;

        if (user.VoiceChannel?.Id == voiceChat.VoiceChannelId)
        {
            await user.ModifyAsync(u => u.Channel = null);
            await ReplyPanelAsync(
                "User kicked",
                $"{user.Mention} has been kicked from this voice chat.",
                Color.LightOrange.RawValue);
        }
    }

    [Command("limit")]
    [Summary("Add a user limit to your VC.")]
    public async Task LimitAsync(uint? limit = null)
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await channel.ModifyAsync(c => c.UserLimit = new Optional<int?>((int?) limit));

        if (limit is null)
            await ReplyPanelAsync("Voice Chat", "Voice user limit reset.");
        else
            await ReplyPanelAsync("Voice Chat", $"User limit set to {Format.Bold(limit + " user(s)")}.", AccentColor);
    }

    [Command("lock")]
    [Summary("Lock the VC. This makes it so no one else can join." +
        " Leaving won't give you permission back unless you're the owner.")]
    public async Task LockAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var overwrite = new OverwritePermissions(connect: PermValue.Deny);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);

        await ReplyPanelAsync(
            "Voice Chat",
            "Voice chat locked.\n" +
            $"-# Use {Format.Code(HuTaoConfig.Configuration.Prefix + "voice unlock")} or the panel to unlock.");
    }

    [Command("owner")]
    [Summary("Show the current owner of the VC.")]
    public async Task OwnerAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null)
            return;

        var owner = Context.Guild.GetUser(voiceChat.UserId);
        await ReplyPanelAsync("Voice Chat", $"The current owner is {Format.Bold(owner.GetFullUsername())}.");
    }

    [Command("reveal")]
    [Summary("Reveals the VC from everyone. This sets the inherit permission on the everyone role.")]
    public async Task RevealAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var overwrite = new OverwritePermissions(viewChannel: PermValue.Inherit);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        await ReplyPanelAsync("Voice Chat", "Voice chat revealed.");
    }

    [Command("transfer")]
    [Summary("Transfer ownership to someone else.")]
    public async Task TransferAsync(IGuildUser user, IVoiceChannel? channel)
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => channel != null
                ? vc.VoiceChannelId == channel.Id
                : vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || user.IsBot || user.IsWebhook)
            return;

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var canManage = Context.User is IGuildUser current && current.GetPermissions(voiceChannel).ManageChannel;
        if (voiceChat.Owner.Id != Context.User.Id && !canManage)
        {
            await ReplyPanelAsync("Voice Chat", "You are not the owner of this voice chat.");
            return;
        }

        if (voiceChat.Owner.Id == user.Id)
        {
            await ReplyPanelAsync("Voice Chat", "That user is already the owner.");
            return;
        }

        await voiceChannel.RemovePermissionOverwriteAsync(Context.User);
        await voiceChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
            manageChannel: PermValue.Allow,
            muteMembers: PermValue.Allow));

        voiceChat.UserId = user.Id;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Voice Chat",
            $"Ownership transferred to {Format.Bold(user.GetFullUsername())}.",
            AccentColor);
    }

    [Command("unban")]
    [Summary("Unban someone from your current VC.")]
    public async Task UnbanAsync(IGuildUser user)
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id || user.Id == Context.User.Id)
            return;

        var voiceChannel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        await voiceChannel.RemovePermissionOverwriteAsync(user);

        var textChannel = (IGuildChannel)Context.Channel;
        await textChannel.RemovePermissionOverwriteAsync(user);

        await ReplyPanelAsync(
            "User unbanned",
            $"{user.Mention} has been unbanned from this voice chat.",
            Color.Blue.RawValue);
    }

    [Command("unlock")]
    [Summary("Unlocks the VC.")]
    public async Task UnlockAsync()
    {
        var voiceChat = await db.Set<VoiceChatLink>().AsQueryable()
            .FirstOrDefaultAsync(vc => vc.TextChannelId == Context.Channel.Id);

        if (voiceChat is null || voiceChat.UserId != Context.User.Id)
            return;

        var channel = Context.Guild.GetVoiceChannel(voiceChat.VoiceChannelId);
        var everyone = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole);
        var overwrite = everyone?.Modify(connect: PermValue.Inherit)
            ?? new OverwritePermissions(connect: PermValue.Inherit);

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, overwrite);
        await ReplyPanelAsync("Voice Chat", "Voice chat unlocked.");
    }

    private async Task ReplyPanelAsync(string title, string body, uint? accentColor = null)
    {
        var container = new ContainerBuilder()
            .WithTextDisplay($"## {title}\n{body}".Truncate(3200))
            .WithAccentColor(accentColor ?? AccentColor);

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Voice Panel", VoiceChatPanelComponentIds.OpenButtonId, ButtonStyle.Primary))
            .Build();

        await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
    }
}