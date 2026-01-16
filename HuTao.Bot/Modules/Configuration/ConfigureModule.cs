using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;
using static Discord.MentionUtils;
using static Humanizer.LetterCasing;
using static HuTao.Data.Models.Moderation.Logging.LogReprimandType;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogChannelConfig;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace HuTao.Bot.Modules.Configuration;

[Group("configure")]
[Alias("config", "configuration", "set", "setting", "settings")]
[Name("Configuration")]
[Summary("Bot Configurations.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ConfigureModule(HuTaoContext db, IMemoryCache cache, ModerationService moderation)
    : ModuleBase<SocketCommandContext>
{
    [Command("censor nicknames")]
    [Alias("censor nickname")]
    [Summary("Whether nicknames should be censored. You must set `name replacement` for this to take effect.")]
    public async Task CensorNicknamesAsync(
        [Summary("Leave empty to toggle")] bool? shouldCensor = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.CensorNicknames = shouldCensor ?? !rules.CensorNicknames;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        await ReplyAsync($"New value: {rules.CensorNicknames}");
    }

    [Command("censor usernames")]
    [Alias("censor username")]
    [Summary("Whether usernames should be censored. You must set `name replacement` for this to take effect.")]
    public async Task CensorUsernamesAsync(
        [Summary("Leave empty to toggle")] bool? shouldCensor = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.CensorUsernames = shouldCensor ?? !rules.CensorUsernames;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        await ReplyAsync($"New value: {rules.CensorUsernames}");
    }

    [Command("auto cooldown")]
    [Summary("Configures the auto moderation cooldown.")]
    public async Task ConfigureAutoCooldownAsync(
        [Summary("Leave empty to disable auto moderation cooldown.")]
        TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.AutoReprimandCooldown = length;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        if (length is null)
            await ReplyAsync("Auto moderation cooldown has been disabled.");
        else
            await ReplyAsync($"Auto moderation cooldown has been set to {Format.Bold(length.Value.Humanize())}");
    }

    [Command("notice expiry")]
    [Summary("Set the time for when a notice is automatically pardoned. This will not affect old cases.")]
    public async Task ConfigureAutoPardonNoticeAsync(
        [Summary("Leave empty to disable auto pardon of notices.")]
        TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.NoticeExpiryLength = length;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        if (length is null)
            await ReplyAsync("Auto-pardon of notices has been disabled.");
        else
            await ReplyAsync($"Notices will now be pardoned after {Format.Bold(length.Value.Humanize())}");
    }

    [Command("warning expiry")]
    [Summary("Set the time for when a warning is automatically pardoned. This will not affect old cases.")]
    public async Task ConfigureAutoPardonWarningAsync(
        [Summary("Leave empty to disable auto pardon of warnings.")]
        TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.WarningExpiryLength = length;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

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

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        await ReplyAsync($"New value: {rules.ReplaceMutes}");
    }

    [Command("censor expiry")]
    [Summary("Set the time for when a censor is considered. This is used for reprimand triggers.")]
    public async Task ConfigureCensorExpiryAsync(
        [Summary("Leave empty to disable censor range.")]
        TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.CensoredExpiryLength = length;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        if (length is null)
            await ReplyAsync("Censor expiry has been disabled.");
        else
            await ReplyAsync($"Censors will now be considered active for {Format.Bold(length.Value.Humanize())}");
    }

    [Command("auto expiry")]
    [Summary("Set the time for when a filter expires. This is used for reprimand triggers.")]
    public async Task ConfigureFilterExpiryAsync(
        [Summary("Leave empty to disable filter range.")]
        TimeSpan? length = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.FilteredExpiryLength = length;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        if (length is null)
            await ReplyAsync("Filter expiry has been disabled.");
        else
            await ReplyAsync($"Filters will now be considered active for {Format.Bold(length.Value.Humanize())}");
    }

    [Command("hard mute")]
    [Summary("Configures the Hard Mute role.")]
    public async Task ConfigureHardMuteAsync(
        [Summary("Optionally provide a mention, ID, or name of an existing role.")]
        IRole? role = null,
        [Summary("`True` if you want to skip setting up permissions.")]
        bool skipPermissions = false,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        await moderation.ConfigureHardMuteRoleAsync(rules, Context.Guild, role, skipPermissions);
        cache.InvalidateCaches(Context.Guild);

        if (role is null)
            await ReplyAsync("Mute role has been configured.");
        else
            await ReplyAsync($"Mute role has been set to {Format.Bold(role.Name)}");
    }

    [Command("mute")]
    [Summary("Configures the Mute role.")]
    public async Task ConfigureMuteAsync(
        [Summary("Optionally provide a mention, ID, or name of an existing role.")]
        IRole? role = null,
        [Summary("`True` if you want to skip setting up permissions.")]
        bool skipPermissions = false,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        await moderation.ConfigureMuteRoleAsync(rules, Context.Guild, role, skipPermissions);
        cache.InvalidateCaches(Context.Guild);

        if (role is null)
            await ReplyAsync("Mute role has been configured.");
        else
            await ReplyAsync($"Mute role has been set to {Format.Bold(role.Name)}");
    }

    [Command("name replacement")]
    [Alias("name replace")]
    [Summary("Set the replacement for censored names.")]
    public async Task ConfigureNameReplacementAsync(
        [Summary("Leave empty to disable")] string? replacement = null,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        rules.NameReplacement = replacement;

        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        if (replacement is null)
            await ReplyAsync("Name replacement has been disabled.");
        else
            await ReplyAsync($"Name replacement has been set to {Format.Bold(replacement)}");
    }

    [Command("voice")]
    [Summary("Configures the Voice Chat settings. Leave the categories empty to use the same one the hub uses.")]
    public async Task ConfigureVoiceChatAsync(
        [Summary(
            "Mention, ID, or name of the hub voice channel that the user can join to create a new voice chat.")]
        IVoiceChannel hubVoiceChannel, VoiceChatOptions? options = null)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);

        if (hubVoiceChannel.CategoryId is null)
            return;

        if (guild.VoiceChatRules is not null)
            db.Remove(guild.VoiceChatRules);

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

        await db.SaveChangesAsync();

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

        await ReplyAsync(
            components: embed.Build().ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None);
    }

    [Command]
    [Summary("View the current settings.")]
    public async Task ViewSettingsAsync(ModerationCategory? category = null)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = await GetRulesAsync(category);

        var mod = rules.Logging;
        var log = guild.LoggingRules;
        var time = guild.GenshinRules;
        var voice = guild.VoiceChatRules;

        var embeds = new List<Embed>
        {
            new EmbedBuilder()
                .WithTitle($"Moderation Configuration for {category?.Name ?? "Default"} category")
                .AddField("Auto Reprimand Cooldown", Length(rules.AutoReprimandCooldown), true)
                .AddField("Filtered Expiry Length", Length(rules.FilteredExpiryLength), true)
                .AddField("Notice Expiry Length", Length(rules.NoticeExpiryLength), true)
                .AddField("Warning Expiry Length", Length(rules.WarningExpiryLength), true)
                .AddField("Censor Expiry Length", Length(rules.CensoredExpiryLength), true)
                .AddField("Censor Nicknames", rules.CensorNicknames, true)
                .AddField("Censor Usernames", rules.CensorUsernames, true)
                .AddField("Name Replacement", rules.NameReplacement.DefaultIfNullOrWhiteSpace("None"), true)
                .AddField("Replace Mutes", rules.ReplaceMutes, true)
                .AddField("Mute Role", Role(rules.MuteRoleId), true)
                .AddField("Hard Mute Role", Role(rules.HardMuteRoleId), true)
                .Build(),
            new EmbedBuilder()
                .WithTitle("Moderation Logging Configuration")
                .AddField("Ignore duplicates", mod?.IgnoreDuplicates ?? false, true)
                .AddField("Default History Reprimands", (mod?.HistoryReprimands ?? All).Humanize(), true)
                .AddField("_ _", "_ _")
                .AddField("History Summary Reprimands", (mod?.SummaryReprimands ?? All).Humanize(), true)
                .AddField("Silent Reprimands", (mod?.SilentReprimands ?? None).Humanize(), true)
                .AddField("_ _", "_ _")
                .AddField(Config("Command Log", mod?.CommandLog, DefaultCommandLogConfig))
                .AddField(Config("Moderator Log", mod?.ModeratorLog, DefaultModeratorLogConfig))
                .AddField("_ _", "_ _")
                .AddField(Config("User Log", mod?.UserLog, DefaultUserLogConfig))
                .AddField(Config("Public Log", mod?.PublicLog, DefaultPublicLogConfig))
                .Build()
        };

        if (log is not null)
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("Logging Configuration")
                .AddItemsIntoFields("Exclusions", log.LoggingExclusions.Select(c => c.ToString() ?? "Unknown"), " ")
                .AddItemsIntoFields("Logging", log.LoggingChannels.GroupBy(l => l.ChannelId), Logging)
                .Build());
        }

        if (time is not null)
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("Genshin Configuration")
                .AddField("Server Status", time.ServerStatus?.JumpUrl ?? "Not configured")
                .AddField(Tracking("America", time.AmericaChannel))
                .AddField(Tracking("Asia", time.AsiaChannel))
                .AddField("_ _", "_ _")
                .AddField(Tracking("Europe", time.EuropeChannel))
                .AddField(Tracking("SAR", time.SARChannel))
                .Build());
        }

        if (voice is not null)
        {
            embeds.Add(new EmbedBuilder()
                .WithTitle("Voice Chat Configuration")
                .AddField("Hub Voice Channel", MentionChannel(voice.HubVoiceChannelId), true)
                .AddField("Voice Channel Category", MentionChannel(voice.VoiceChannelCategoryId), true)
                .AddField("Voice Chat Category", MentionChannel(voice.VoiceChatCategoryId), true)
                .AddField("Purge Empty Voice Chats", voice.PurgeEmpty, true)
                .AddField("Show Join/Leave", voice.ShowJoinLeave, true)
                .AddField("Deletion Delay", Length(voice.DeletionDelay), true)
                .Build());
        }

        const uint defaultAccentColor = 0x9B59FF;
        var componentBuilder = new ComponentBuilderV2();
        foreach (var embed in embeds)
        {
            componentBuilder.WithContainer(new ContainerBuilder()
                .WithSection(embed.ToComponentsV2Section(maxChars: 3800))
                .WithAccentColor(embed.Color?.RawValue ?? defaultAccentColor));
        }

        await ReplyAsync(
            components: componentBuilder.Build(),
            allowedMentions: AllowedMentions.None);

        static EmbedFieldBuilder Config<T>(string name, T? current, T template) where T : ModerationLogConfig
        {
            var config = new LogConfig<T>(current, template);
            var builder = new StringBuilder()
                .AppendLine($"> Log Status: `{config.LogReprimandStatus.Humanize()}`")
                .AppendLine($"> Log Reprimands: `{config.LogReprimands.Humanize()}`")
                .AppendLine($"> Show Appeal: `{config.ShowAppealOnReprimands.Humanize()}`")
                .AppendLine($"> Log Options: `{config.Options.Humanize()}`")
                .AppendLine($"> Appeal Message: `{config.AppealMessage?.Truncate(256) ?? "None"}`");

            if (config is LogConfig<ModerationLogChannelConfig>)
                builder.AppendLine($"> Channel: {config.MentionChannel}");

            return new EmbedFieldBuilder().WithName(name).WithValue(builder).WithIsInline(true);
        }

        static string Logging(IGrouping<ulong, EnumChannel<LogType>> g)
            => $"{MentionChannel(g.Key)}: {g.Humanize(l => l.Type.Humanize(Title))}";

        static EmbedFieldBuilder Tracking(string title, IChannelEntity? tracking) => new EmbedFieldBuilder()
            .WithName($"{title} Server Time").WithIsInline(true)
            .WithValue(tracking?.MentionChannel() ?? "Not configured");

        static string Length(TimeSpan? length) => length?.Humanize() ?? "Indefinite";

        static string Role(ulong? role) => role is null ? "None" : $"{MentionRole(role.Value)} ({role})";
    }

    private async Task<IModerationRules> GetRulesAsync(ModerationCategory? category)
    {
        if (category is not null) return category;
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
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