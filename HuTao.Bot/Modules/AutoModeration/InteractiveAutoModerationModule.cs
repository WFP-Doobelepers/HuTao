using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.AutoModeration;

[Group("auto", "Manage auto-moderation rules.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveAutoModerationModule(HuTaoContext db, ModerationService moderation, IMemoryCache cache)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("add", "Add a spam filter with no punishment.")]
    public Task AddAsync(
        FilterType type,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var config = GetConfiguration(options, type, null);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("warn", "Add a spam filter that warns the user.")]
    public Task AddWarningAsync(
        FilterType type,
        [Summary(description: "Number of warnings to give.")] uint count = 1,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var reprimand = new WarningAction(count);
        var config = GetConfiguration(options, type, reprimand);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("mute", "Add a spam filter that mutes the user.")]
    public Task AddMuteAsync(
        FilterType type,
        TimeSpan? mute_length = null,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var reprimand = new MuteAction(mute_length);
        var config = GetConfiguration(options, type, reprimand);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("kick", "Add a spam filter that kicks the user.")]
    public Task AddKickAsync(
        FilterType type,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var reprimand = new KickAction();
        var config = GetConfiguration(options, type, reprimand);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("ban", "Add a spam filter that bans the user.")]
    public Task AddBanAsync(
        FilterType type,
        TimeSpan? ban_length = null,
        [Summary(description: "Days of messages to delete on ban.")] int delete_days = 1,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var reprimand = new BanAction((uint)Math.Clamp(delete_days, 0, 7), ban_length);
        var config = GetConfiguration(options, type, reprimand);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("note", "Add a spam filter that logs a note.")]
    public Task AddNoteAsync(
        FilterType type,
        [Summary(description: "Number of messages before the rule triggers.")] int amount = 1,
        [Summary(description: "Time window to count messages in.")] TimeSpan? period = null,
        bool delete_messages = true,
        bool global = false,
        int minimum_length = 0,
        string? reason = null,
        TimeSpan? cooldown = null,
        TriggerMode mode = TriggerMode.Retroactive,
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        var options = BuildOptions(amount, period ?? 10.Seconds(), delete_messages, global, minimum_length, reason,
            cooldown, mode, category);
        var reprimand = new NoteAction();
        var config = GetConfiguration(options, type, reprimand);
        return AddConfigurationAsync(config, ephemeral);
    }

    [SlashCommand("list", "View all auto-moderation rules.")]
    public async Task ListAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var configs = await GetCollectionAsync();

        if (configs.Count == 0)
        {
            await FollowupAsync("No auto-moderation rules configured.", ephemeral: true);
            return;
        }

        var embeds = configs
            .Take(10)
            .Select(c => EntityViewer(c).Build())
            .ToArray();

        await FollowupAsync(embeds: embeds, ephemeral: ephemeral);
    }

    [SlashCommand("toggle", "Enable or disable an auto-moderation rule.")]
    public async Task ToggleAsync(
        [Summary(description: "The rule to modify.")] [Autocomplete(typeof(AutoConfigAutocomplete))]
        string id,
        bool? state = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var configs = await GetCollectionAsync();
        var entity = configs.FirstOrDefault(c => c.Id.ToString() == id);

        if (entity is null)
        {
            await FollowupAsync("Rule not found.", ephemeral: true);
            return;
        }

        await moderation.ToggleTriggerAsync(entity, (IGuildUser)Context.User, state);
        cache.InvalidateCaches(Context.Guild);

        await FollowupAsync(
            components: EntityViewer(entity).Build().ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    [SlashCommand("delete", "Delete an auto-moderation rule and its records.")]
    public async Task DeleteAsync(
        [Summary(description: "The rule to delete.")] [Autocomplete(typeof(AutoConfigAutocomplete))]
        string id,
        bool silent = false,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var configs = await GetCollectionAsync();
        var entity = configs.FirstOrDefault(c => c.Id.ToString() == id);

        if (entity is null)
        {
            await FollowupAsync("Rule not found.", ephemeral: true);
            return;
        }

        await moderation.DeleteTriggerAsync(entity, (IGuildUser)Context.User, silent);
        cache.InvalidateCaches(Context.Guild);

        await FollowupAsync($"Rule `{id}` deleted.", ephemeral: ephemeral);
    }

    private static SlashAutoOptions BuildOptions(
        int amount, TimeSpan period, bool deleteMessages, bool global,
        int minimumLength, string? reason, TimeSpan? cooldown,
        TriggerMode mode, ModerationCategory? category) => new()
    {
        Amount = (uint)Math.Clamp(amount, 1, 100),
        TimePeriod = period,
        DeleteMessages = deleteMessages,
        GlobalFilter = global,
        MinimumLength = minimumLength,
        Reason = reason,
        Cooldown = cooldown,
        Mode = mode,
        Category = category
    };

    private static AutoConfiguration GetConfiguration(
        IAutoConfigurationOptions options, FilterType type,
        ReprimandAction? reprimand) => type switch
    {
        FilterType.Messages    => new MessageConfiguration(reprimand, options),
        FilterType.Duplicates  => new DuplicateConfiguration(reprimand, options),
        FilterType.Attachments => new AttachmentConfiguration(reprimand, options),
        FilterType.Emojis      => new EmojiConfiguration(reprimand, options),
        FilterType.Invites     => new InviteConfiguration(reprimand, options),
        FilterType.Links       => new LinkConfiguration(reprimand, options),
        FilterType.Mentions    => new MentionConfiguration(reprimand, options),
        FilterType.NewLines    => new NewLineConfiguration(reprimand, options),
        _                      => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown filter type.")
    };

    private static EmbedBuilder EntityViewer(AutoConfiguration entity)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{entity.GetTitle()} Spam Filter: {entity.Id}")
            .WithDescription(entity.GetDetails())
            .WithColor(entity.Reprimand?.GetColor() ?? Color.Default)
            .AddField("Moderator", entity.GetModerator(), true)
            .AddField("Global", entity.Global, true)
            .AddField("Delete Messages", entity.DeleteMessages, true)
            .AddField("Time Period", entity.Length.Humanize(), true)
            .AddField("Limit", entity.Amount, true)
            .AddField("Minimum Length", entity.MinimumLength, true)
            .AddField("Action", entity.Reprimand?.ToString() ?? "None", true)
            .AddField("Cooldown", entity.Cooldown?.Humanize() ?? "Default", true)
            .AddField("Category", entity.Category?.Name ?? "Default", true)
            .WithTimestamp(entity);

        return entity switch
        {
            DuplicateConfiguration config => embed
                .WithTitle($"{entity.GetTitle()} {config.Type} Spam Filter: {entity.Id}")
                .AddField("Tolerance", config.Tolerance, true)
                .AddField("Percentage", $"{config.Percentage:P}", true)
                .AddField("Type", config.Type, true),
            MentionConfiguration config => embed
                .AddField("Count Duplicate", config.CountDuplicate, true)
                .AddField("Count Invalid", config.CountInvalid, true)
                .AddField("Count Role Members", config.CountRoleMembers, true),
            NewLineConfiguration config => embed.AddField("Blank Lines Only", config.BlankOnly, true),
            _ => embed
        };
    }

    private async Task AddConfigurationAsync(AutoConfiguration configuration, bool ephemeral)
    {
        await DeferAsync(ephemeral);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        configuration.Length = configuration.Length.Clamp(1.Seconds(), 1.Hours());
        configuration.Amount = Math.Clamp(configuration.Amount, 1, 100);

        rules.Triggers.Add(configuration.WithModerator(Context));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        var embed = EntityViewer(configuration)
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
            .Build();

        var container = new ContainerBuilder()
            .WithSection(embed.ToComponentsV2Section(maxChars: 3800))
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay("-# Auto-moderation rule created successfully.")
            .WithAccentColor(AccentColor);

        var actions = new ActionRowBuilder()
            .WithButton(new ButtonBuilder("Toggle", $"auto-toggle:{configuration.Id}", ButtonStyle.Secondary))
            .WithButton(new ButtonBuilder("Delete", $"auto-delete:{configuration.Id}", ButtonStyle.Danger));

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(actions)
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    private async Task<IList<AutoConfiguration>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.Triggers.OfType<AutoConfiguration>().ToList();
    }

    private class SlashAutoOptions : IAutoConfigurationOptions
    {
        public bool DeleteMessages { get; set; } = true;
        public bool GlobalFilter { get; set; }
        public bool MentionCountDuplicates { get; set; }
        public bool MentionCountInvalid { get; set; }
        public bool MentionCountRoleMembers { get; set; }
        public bool NewLineBlankOnly { get; set; }
        public double DuplicatePercentage { get; set; }
        public DuplicateConfiguration.DuplicateType DuplicateType { get; set; } = DuplicateConfiguration.DuplicateType.Message;
        public int DuplicateTolerance { get; set; }
        public int MinimumLength { get; set; }
        public string? Reason { get; set; }
        public TimeSpan TimePeriod { get; set; }
        public TimeSpan? Cooldown { get; set; }
        public ModerationCategory? Category { get; set; }
        public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;
        public uint Amount { get; set; } = 1;
    }
}
