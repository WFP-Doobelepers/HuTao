using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Configuration;

[Group("configure")]
[Name("Configuration")]
[Summary("Bot Configurations.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ConfigureModule : ModuleBase<SocketCommandContext>
{
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public ConfigureModule(ModerationService moderation, HuTaoContext db)
    {
        _moderation = moderation;
        _db         = db;
    }

    [Command("notice expiry")]
    [Summary("Set the time for when a notice is automatically hidden. This will not affect old cases.")]
    public async Task ConfigureAutoPardonNoticeAsync(
        [Summary("Leave empty to disable auto pardon of notices.")] TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.NoticeExpiryLength = length;
        await _db.SaveChangesAsync();

        if (length is null)
            await ReplyAsync("Auto-pardon of notices has been disabled.");
        else
            await ReplyAsync($"Notices will now be pardoned after {Format.Bold(length.Value.Humanize())}");
    }

    [Command("warning expiry")]
    [Summary("Set the time for when a warning is automatically hidden. This will not affect old cases.")]
    public async Task ConfigureAutoPardonWarningAsync(
        [Summary("Leave empty to disable auto pardon of warnings.")] TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.WarningExpiryLength = length;
        await _db.SaveChangesAsync();

        if (length is null)
            await ReplyAsync("Auto-pardon of warnings has been disabled.");
        else
            await ReplyAsync($"Warnings will now be pardoned after {Format.Bold(length.Value.Humanize())}");
    }

    [Command("replace mutes")]
    [Alias("replace mute")]
    [Summary("Whether mutes should be replaced when there is an active one.")]
    public async Task ConfigureAutoPardonWarningAsync(
        [Summary("Leave empty to toggle")] bool? shouldReplace = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.ReplaceMutes = shouldReplace ?? !rules.ReplaceMutes;
        await _db.SaveChangesAsync();

        await ReplyAsync($"New value: {rules.ReplaceMutes}");
    }

    [Command("censor range")]
    [Summary("Set the time for when a censor is considered.")]
    public async Task ConfigureCensorTimeRangeAsync(
        [Summary("Leave empty to disable censor range.")] TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.CensorTimeRange = length;
        await _db.SaveChangesAsync();

        if (length is null)
            await ReplyAsync("Censor range has been disabled.");
        else
            await ReplyAsync($"Censors will now be considered active for {Format.Bold(length.Value.Humanize())}");
    }

    [Command("mute")]
    [Summary("Configures the Mute role.")]
    public async Task ConfigureMuteAsync(
        [Summary("Optionally provide a mention, ID, or name of an existing role.")] IRole? role = null,
        [Summary("True if you want to skip setting up permissions.")]
        bool skipPermissions = false,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        await _moderation.ConfigureMuteRoleAsync(rules, Context.Guild, role, skipPermissions);

        if (role is null)
            await ReplyAsync("Mute role has been configured.");
        else
            await ReplyAsync($"Mute role has been set to {Format.Bold(role.Name)}");
    }

    [Command("voice")]
    [Summary("Configures the Voice Chat settings. Leave the categories empty to use the same one the hub uses.")]
    public async Task ConfigureVoiceChatAsync(
        [Summary(
            "Mention, ID, or name of the hub voice channel that the user can join to create a new voice chat.")]
        IVoiceChannel hubVoiceChannel, VoiceChatOptions? options = null)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

        if (hubVoiceChannel.CategoryId is null)
            return;

        if (guild.VoiceChatRules is not null)
            _db.Remove(guild.VoiceChatRules);

        guild.VoiceChatRules = new VoiceChatRules
        {
            GuildId                = guild.Id,
            HubVoiceChannelId      = hubVoiceChannel.Id,
            VoiceChannelCategoryId = options?.VoiceChannelCategory?.Id ?? hubVoiceChannel.CategoryId.Value,
            VoiceChatCategoryId    = options?.VoiceChatCategory?.Id ?? hubVoiceChannel.CategoryId.Value,
            DeletionDelay          = options?.DeleteDelay ?? TimeSpan.Zero,
            PurgeEmpty             = options?.PurgeEmpty ?? true,
            ShowJoinLeave          = options?.ShowJoinLeave ?? true
        };

        await _db.SaveChangesAsync();

        var embed = new EmbedBuilder()
            .WithTitle("Voice Chat settings")
            .WithDescription("Voice Chat Hub settings have been configured with the following:")
            .WithColor(Color.Green)
            .AddField("Hub Voice Channel: ", $"<#{hubVoiceChannel.Id}>")
            .AddField("Voice Channel Category: ", $"<#{guild.VoiceChatRules.VoiceChannelCategoryId}>")
            .AddField("Voice Chat Category: ", $"<#{guild.VoiceChatRules.VoiceChatCategoryId}>")
            .AddField("Purge Empty Voice Chats: ", guild.VoiceChatRules.PurgeEmpty)
            .AddField("Show Join-Leave: ", guild.VoiceChatRules.ShowJoinLeave)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    private async Task<IModerationRules> GetRulesAsync(ModerationCategory? category)
    {
        if (category is not null) return category;
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationRules ??= new ModerationRules();
    }

    [NamedArgumentType]
    public class VoiceChatOptions
    {
        [HelpSummary("Purge empty channels after all members leave.")]
        public bool PurgeEmpty { get; init; } = true;

        [HelpSummary("Show join messages.")] public bool ShowJoinLeave { get; init; } = true;

        [HelpSummary("The category where Voice Channels will be made.")]
        public ICategoryChannel? VoiceChannelCategory { get; init; }

        [HelpSummary("The category where Voice Chat channels will be made.")]
        public ICategoryChannel? VoiceChatCategory { get; init; }

        [HelpSummary("The time to wait before deleting an empty channel.")]
        public TimeSpan? DeleteDelay { get; set; } = TimeSpan.Zero;
    }
}