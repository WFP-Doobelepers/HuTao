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
    [Summary("Set the time for when a notice is automatically pardoned. This will not affect old cases.")]
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
    [Summary("Set the time for when a warning is automatically pardoned. This will not affect old cases.")]
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

    [Command("hard mute")]
    [Summary("Configures the Hard Mute role.")]
    public async Task ConfigureHardMuteAsync(
        [Summary("Optionally provide a mention, ID, or name of an existing role.")]
        IRole? role = null,
        [Summary("True if you want to skip setting up permissions.")]
        bool skipPermissions = false,
        ModerationCategory? category = null)
    {
        var rules = await GetRulesAsync(category);
        await _moderation.ConfigureHardMuteRoleAsync(rules, Context.Guild, role, skipPermissions);

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

    [Command]
    [Summary("View the current settings.")]
    public async Task ViewSettingsAsync(ModerationCategory? category = null)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = await GetRulesAsync(category);

        var mod = rules.Logging;
        var log = guild.LoggingRules;
        var time = guild.GenshinRules;
        var voice = guild.VoiceChatRules;

        var embeds = new List<Embed>
        {
            new EmbedBuilder()
                .WithTitle($"Moderation Configuration for {category?.Name ?? "Default"} category")
                .AddField("Replace Mutes", rules.ReplaceMutes, true)
                .AddField("Censor Time Range", Length(rules.CensorTimeRange), true)
                .AddField("Notice Expiry Length", Length(rules.NoticeExpiryLength), true)
                .AddField("Warning Expiry Length", Length(rules.WarningExpiryLength), true)
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
                .Build(),
            new EmbedBuilder()
                .WithTitle("Logging Configuration")
                .AddItemsIntoFields("Exclusions", log?.LoggingExclusions.Select(c => c.ToString() ?? "Unknown"), " ")
                .AddItemsIntoFields("Logging", log?.LoggingChannels.GroupBy(l => l.ChannelId), Logging)
                .Build()
        };

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

        await ReplyAsync(embeds: embeds.ToArray());

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

        static EmbedFieldBuilder Tracking(string title, IChannelEntity? tracking)
            => new EmbedFieldBuilder()
                .WithName($"{title} Server Time").WithIsInline(true)
                .WithValue(tracking?.MentionChannel() ?? "Not configured");

        static string Length(TimeSpan? length) => length?.Humanize() ?? "Indefinite";

        static string Role(ulong? role) => role is null ? "None" : $"{MentionRole(role.Value)} ({role})";
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