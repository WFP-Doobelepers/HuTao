using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace HuTao.Bot.Modules.Logging;

[Group("log", "Logging configuration.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveLoggingModule(HuTaoContext db) : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    public enum LoggingChannelContext
    {
        Moderator,
        Public
    }

    public enum LoggingContext
    {
        Command,
        User,
        Moderator,
        Public
    }

    [SlashCommand("reprimand", "Choose which reprimand types are logged.")]
    public async Task ConfigureReprimandAsync(
        LoggingContext context,
        LogReprimandType type,
        bool? state = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        if (type is not LogReprimandType.None)
        {
            config.LogReprimands ??= LogReprimandType.None;
            config.LogReprimands = config.LogReprimands.Value.SetValue(type, state);
            await db.SaveChangesAsync();
        }

        await ReplyPanelAsync(
            "Logged Reprimands",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(config.LogReprimands ?? LogReprimandType.None).Humanize()}", ephemeral);
    }

    [SlashCommand("status", "Choose which status changes are logged.")]
    public async Task ConfigureStatusAsync(
        LoggingContext context,
        LogReprimandStatus type,
        bool? state = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        if (type is not LogReprimandStatus.None)
        {
            config.LogReprimandStatus ??= LogReprimandStatus.None;
            config.LogReprimandStatus = config.LogReprimandStatus.Value.SetValue(type, state);
            await db.SaveChangesAsync();
        }

        await ReplyPanelAsync(
            "Logged Status",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(config.LogReprimandStatus ?? LogReprimandStatus.None).Humanize()}", ephemeral);
    }

    [SlashCommand("rules", "Configure the moderation logging options.")]
    public async Task ConfigureRulesAsync(
        LoggingContext context,
        ModerationLogOptions type = ModerationLogOptions.None,
        bool? state = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        if (type is not ModerationLogOptions.None)
        {
            config.Options ??= ModerationLogOptions.None;
            config.Options = config.Options.Value.SetValue(type, state);
            await db.SaveChangesAsync();
        }

        await ReplyPanelAsync(
            "Moderation Logging Options",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(config.Options ?? ModerationLogOptions.None).Humanize()}", ephemeral);
    }

    [SlashCommand("appeal", "Show the appeal message for specific reprimand types.")]
    public async Task ConfigureAppealAsync(
        LoggingContext context,
        LogReprimandType type = LogReprimandType.None,
        bool? show = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        if (type is not LogReprimandType.None)
        {
            config.ShowAppealOnReprimands ??= LogReprimandType.None;
            config.ShowAppealOnReprimands = config.ShowAppealOnReprimands.Value.SetValue(type, show);
            await db.SaveChangesAsync();
        }

        await ReplyPanelAsync(
            "Appeal Visibility",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(config.ShowAppealOnReprimands ?? LogReprimandType.None).Humanize()}", ephemeral);
    }

    [SlashCommand("appeal-message", "Set the message shown to users when they receive a reprimand.")]
    public async Task ConfigureAppealMessageAsync(
        LoggingContext context,
        string? message = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        config.AppealMessage = message;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            message is null ? "Appeal Message Cleared" : "Appeal Message Set",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(message is null ? "Disabled" : message.Truncate(500))}", ephemeral);
    }

    [SlashCommand("silent", "Automatically hide the command response for these reprimand types.")]
    public async Task ConfigureSilentAsync(
        LogReprimandType type = LogReprimandType.None,
        bool? state = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var rules = await GetLoggingAsync(category);
        if (type is not LogReprimandType.None)
        {
            rules.SilentReprimands ??= LogReprimandType.None;
            rules.SilentReprimands = rules.SilentReprimands.Value.SetValue(type, state);
            await db.SaveChangesAsync();
        }

        await ReplyPanelAsync(
            "Silent Reprimands",
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(rules.SilentReprimands ?? LogReprimandType.None).Humanize()}", ephemeral);
    }

    [SlashCommand("channel", "Set the channel where reprimand logs are posted.")]
    public async Task ConfigureChannelAsync(
        LoggingChannelContext context,
        ITextChannel channel,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await GetConfigAsync(category, context);
        config.ChannelId = channel.Id;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Moderation Log Channel",
            $"**Context:** {context}\n" +
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Channel:** {config.MentionChannel()}", ephemeral);
    }

    [SlashCommand("event", "Enable or disable specific events to be logged.")]
    public async Task ConfigureEventAsync(
        LogType type,
        ITextChannel? channel = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        var rules = guild.LoggingRules.LoggingChannels;
        db.RemoveRange(rules.Where(rule => rule.Type == type));

        if (channel is not null)
            rules.Add(new EnumChannel<LogType>(type, channel));

        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Log Events",
            channel is not null
                ? $"**Event:** {type}\n**Channel:** {channel.Mention}"
                : $"**Event:** {type}\n**Channel:** Disabled", ephemeral);
    }

    [SlashCommand("attachments", "Re-upload attachments when messages are deleted.")]
    public async Task ConfigureAttachmentsAsync(
        bool? enabled = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();
        guild.LoggingRules.UploadAttachments = enabled ?? !guild.LoggingRules.UploadAttachments;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Re-upload Attachments",
            $"**Current:** {guild.LoggingRules.UploadAttachments}", ephemeral);
    }

    [SlashCommand("history-reprimands", "Set which reprimand types appear in user histories by default.")]
    public async Task HistoryReprimandsAsync(
        LogReprimandType? type = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var rules = await GetLoggingAsync(category);
        rules.HistoryReprimands = type;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "History Reprimands",
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(rules.HistoryReprimands is null ? "Default" : rules.HistoryReprimands.Value.Humanize())}", ephemeral);
    }

    [SlashCommand("ignore-duplicates", "Ignore duplicate moderation logs.")]
    public async Task IgnoreDuplicatesAsync(
        bool? state = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var rules = await GetLoggingAsync(category);
        rules.IgnoreDuplicates = state ?? !rules.IgnoreDuplicates;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Ignore Duplicates",
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {rules.IgnoreDuplicates}", ephemeral);
    }

    [SlashCommand("summary-reprimands", "Set which reprimand types appear in user summaries by default.")]
    public async Task SummaryReprimandsAsync(
        LogReprimandType? type = null,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var rules = await GetLoggingAsync(category);
        rules.SummaryReprimands = type;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Summary Reprimands",
            $"**Scope:** {ScopeName(category)}\n" +
            $"**Current:** {(rules.SummaryReprimands is null ? "Default" : rules.SummaryReprimands.Value.Humanize())}", ephemeral);
    }

    [SlashCommand("overview", "View a summary of all logging configuration.")]
    public async Task OverviewAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var rules = await GetLoggingAsync(category);
        var scope = ScopeName(category);

        var commandLog = rules.CommandLog ?? new ModerationLogConfig();
        var userLog = rules.UserLog ?? new ModerationLogConfig();
        var modLog = rules.ModeratorLog ?? new ModerationLogChannelConfig();
        var publicLog = rules.PublicLog ?? new ModerationLogChannelConfig();

        var body = string.Join("\n",
            $"**Scope:** {scope}",
            "",
            "### Channels",
            $"**Moderator:** {modLog.MentionChannel()}",
            $"**Public:** {publicLog.MentionChannel()}",
            "",
            "### Logged Reprimands",
            $"**Command:** {(commandLog.LogReprimands ?? LogReprimandType.None).Humanize()}",
            $"**User:** {(userLog.LogReprimands ?? LogReprimandType.None).Humanize()}",
            $"**Moderator:** {(modLog.LogReprimands ?? LogReprimandType.None).Humanize()}",
            $"**Public:** {(publicLog.LogReprimands ?? LogReprimandType.None).Humanize()}",
            "",
            "### Options",
            $"**Silent:** {(rules.SilentReprimands ?? LogReprimandType.None).Humanize()}",
            $"**Ignore Duplicates:** {rules.IgnoreDuplicates}",
            $"**History Default:** {(rules.HistoryReprimands is null ? "Default" : rules.HistoryReprimands.Value.Humanize())}",
            $"**Summary Default:** {(rules.SummaryReprimands is null ? "Default" : rules.SummaryReprimands.Value.Humanize())}");

        await ReplyPanelAsync("Logging Overview", body, ephemeral);
    }

    private async Task ReplyPanelAsync(string title, string body, bool ephemeral = false)
    {
        var container = new ContainerBuilder()
            .WithTextDisplay($"## {title}\n{body}".Truncate(3800))
            .WithAccentColor(AccentColor);

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton(new ButtonBuilder("Open Config Panel", "cfg:open", ButtonStyle.Primary))
                .WithButton(new ButtonBuilder("Logging Exclusions", "logex:open", ButtonStyle.Secondary)))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    private static string ScopeName(ModerationCategory? category)
        => category?.Name ?? "Global (Default)";

    private async Task<IModerationRules> GetRulesAsync(ModerationCategory? category)
    {
        if (category is not null) return category;
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationRules ??= new ModerationRules();
    }

    private async Task<IChannelEntity> GetConfigAsync(ModerationCategory? category, LoggingChannelContext context)
    {
        var rules = await GetLoggingAsync(category);
        return context switch
        {
            LoggingChannelContext.Moderator => rules.ModeratorLog ??= new ModerationLogChannelConfig(),
            LoggingChannelContext.Public    => rules.PublicLog ??= new ModerationLogChannelConfig(),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Invalid logging context.")
        };
    }

    private async Task<ModerationLogConfig> GetConfigAsync(ModerationCategory? category, LoggingContext context)
    {
        var rules = await GetLoggingAsync(category);
        return context switch
        {
            LoggingContext.Command   => rules.CommandLog ??= new ModerationLogConfig(),
            LoggingContext.User      => rules.UserLog ??= new ModerationLogConfig(),
            LoggingContext.Moderator => rules.ModeratorLog ??= new ModerationLogChannelConfig(),
            LoggingContext.Public    => rules.PublicLog ??= new ModerationLogChannelConfig(),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Invalid logging context.")
        };
    }

    private async Task<ModerationLoggingRules> GetLoggingAsync(ModerationCategory? category)
    {
        var rules = await GetRulesAsync(category);
        return rules.Logging ??= new ModerationLoggingRules();
    }
}
