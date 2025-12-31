using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Extensions;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Configuration;

[RequireContext(ContextType.Guild)]
public class InteractiveConfigurationModule(
    HuTaoContext db,
    IMemoryCache cache,
    ModerationService moderation,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string SectionSelectId = "cfg:section";
    private const string CategorySelectId = "cfg:category";
    private const string SettingSelectId = "cfg:setting";

    private const string BackButtonId = "cfg:back";
    private const string RefreshButtonId = "cfg:refresh";

    private const string RoleSelectId = "cfg:role";
    private const string ClearRoleId = "cfg:clear-role";
    private const string ToggleSkipPermissionsId = "cfg:skip-perms";

    private const string TimeSpanModalPrefix = "cfg:ts";
    private const string StringModalPrefix = "cfg:str";

    private const string KeyAutoCooldown = "m:auto-cooldown";
    private const string KeyFilteredExpiry = "m:filtered-expiry";
    private const string KeyNoticeExpiry = "m:notice-expiry";
    private const string KeyWarningExpiry = "m:warning-expiry";
    private const string KeyCensorExpiry = "m:censor-expiry";
    private const string KeyCensorNicknames = "m:censor-nicknames";
    private const string KeyCensorUsernames = "m:censor-usernames";
    private const string KeyNameReplacement = "m:name-replacement";
    private const string KeyReplaceMutes = "m:replace-mutes";
    private const string KeyMuteRole = "m:mute-role";
    private const string KeyHardMuteRole = "m:hard-mute-role";

    [SlashCommand("config", "Open the configuration panel.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ConfigAsync(
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope]
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var state = ConfigPanelState.Create(guild, Context.Guild.Name, category);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(1)
            .WithUserState(state)
            .WithPageFactory(GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(SectionSelectId, true)]
    public async Task SelectSectionAsync(string section)
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        if (!Enum.TryParse(section, ignoreCase: true, out ConfigSection selected))
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.Section = selected;
        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(CategorySelectId, true)]
    public async Task SelectCategoryAsync(string categoryId)
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;

        if (categoryId == ConfigPanelState.GlobalCategoryValue)
        {
            state.CategoryId = null;
        }
        else if (Guid.TryParse(categoryId, out var parsed))
        {
            state.CategoryId = parsed;
        }

        await SyncModerationSnapshotAsync(state);

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(SettingSelectId, true)]
    public async Task SelectSettingAsync(string settingKey)
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out var interaction))
            return;

        if (state.Section is not ConfigSection.Moderation)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        state.PendingSettingKey = settingKey;

        if (settingKey is KeyMuteRole or KeyHardMuteRole)
        {
            state.View = ConfigView.RoleSelect;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (settingKey is KeyNameReplacement)
        {
            await RespondWithStringModalAsync(interaction, state, settingKey);
            return;
        }

        if (settingKey is KeyAutoCooldown
            or KeyFilteredExpiry
            or KeyNoticeExpiry
            or KeyWarningExpiry
            or KeyCensorExpiry)
        {
            await RespondWithTimeSpanModalAsync(interaction, state, settingKey);
            return;
        }

        await DeferAsync();

        var updated = await TryApplyModerationToggleAsync(state, settingKey);
        if (updated)
        {
            cache.InvalidateCaches(Context.Guild);
            await db.SaveChangesAsync();
            state.LastUpdated = DateTimeOffset.UtcNow;
            await SyncModerationSnapshotAsync(state);
        }

        await RenderAsync(paginator);
    }

    [ComponentInteraction(BackButtonId, true)]
    public async Task BackAsync()
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RefreshButtonId, true)]
    public async Task RefreshAsync()
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        await SyncModerationSnapshotAsync(state);
        state.LastUpdated = DateTimeOffset.UtcNow;

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(ToggleSkipPermissionsId, true)]
    public async Task ToggleSkipPermissionsAsync()
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        state.SkipRolePermissionSetup = !state.SkipRolePermissionSetup;

        await DeferAsync();
        await RenderAsync(paginator);
    }

    [ComponentInteraction(RoleSelectId, true)]
    public async Task SelectRoleAsync(IRole[] roles)
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        var role = roles.FirstOrDefault();
        if (role is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (state.PendingSettingKey is not (KeyMuteRole or KeyHardMuteRole))
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
        {
            await RenderAsync(paginator);
            return;
        }

        if (state.PendingSettingKey == KeyMuteRole)
            await moderation.ConfigureMuteRoleAsync(rules, Context.Guild, role, state.SkipRolePermissionSetup);
        else
            await moderation.ConfigureHardMuteRoleAsync(rules, Context.Guild, role, state.SkipRolePermissionSetup);

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        state.View = ConfigView.Overview;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncModerationSnapshotAsync(state);

        await RenderAsync(paginator);
    }

    [ComponentInteraction(ClearRoleId, true)]
    public async Task ClearRoleAsync()
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        if (state.PendingSettingKey is not (KeyMuteRole or KeyHardMuteRole))
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
        {
            await RenderAsync(paginator);
            return;
        }

        if (state.PendingSettingKey == KeyMuteRole)
            rules.MuteRoleId = null;
        else
            rules.HardMuteRoleId = null;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        state.View = ConfigView.Overview;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncModerationSnapshotAsync(state);

        await RenderAsync(paginator);
    }

    [ModalInteraction($"{TimeSpanModalPrefix}:*", true)]
    public async Task UpdateTimeSpanAsync(string settingKey, ConfigTimeSpanModal modal)
    {
        if (!TryGetPanelFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
        {
            await RenderAsync(paginator);
            return;
        }

        switch (settingKey)
        {
            case KeyAutoCooldown:
                rules.AutoReprimandCooldown = modal.Value;
                break;
            case KeyFilteredExpiry:
                rules.FilteredExpiryLength = modal.Value;
                break;
            case KeyNoticeExpiry:
                rules.NoticeExpiryLength = modal.Value;
                break;
            case KeyWarningExpiry:
                rules.WarningExpiryLength = modal.Value;
                break;
            case KeyCensorExpiry:
                rules.CensoredExpiryLength = modal.Value;
                break;
            default:
                await RenderAsync(paginator);
                return;
        }

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        state.View = ConfigView.Overview;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncModerationSnapshotAsync(state);

        await RenderAsync(paginator);
    }

    [ModalInteraction($"{StringModalPrefix}:*", true)]
    public async Task UpdateStringAsync(string settingKey, ConfigStringModal modal)
    {
        if (!TryGetPanelFromModal(out var paginator, out var state))
        {
            await RespondAsync("This panel is no longer active.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
        {
            await RenderAsync(paginator);
            return;
        }

        switch (settingKey)
        {
            case KeyNameReplacement:
                rules.NameReplacement = string.IsNullOrWhiteSpace(modal.Value) ? null : modal.Value;
                break;
            default:
                await RenderAsync(paginator);
                return;
        }

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        state.View = ConfigView.Overview;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncModerationSnapshotAsync(state);

        await RenderAsync(paginator);
    }

    private bool TryGetPanelFromComponent(
        out IComponentPaginator paginator,
        out ConfigPanelState state,
        out IComponentInteraction interaction)
    {
        if (Context.Interaction is not IComponentInteraction i
            || !interactive.TryGetComponentPaginator(i.Message, out var p)
            || p is null
            || !p.CanInteract(i.User))
        {
            paginator = null!;
            state = null!;
            interaction = null!;
            return false;
        }

        interaction = i;
        paginator = p;
        state = paginator.GetUserState<ConfigPanelState>();
        return true;
    }

    private bool TryGetPanelFromModal(out IComponentPaginator paginator, out ConfigPanelState state)
    {
        if (Context.Interaction is not SocketModal modal
            || !interactive.TryGetComponentPaginator(modal.Message, out var p)
            || p is null
            || !p.CanInteract(modal.User))
        {
            paginator = null!;
            state = null!;
            return false;
        }

        paginator = p;
        state = paginator.GetUserState<ConfigPanelState>();
        return true;
    }

    private static IPage GeneratePage(IComponentPaginator p)
    {
        var state = p.GetUserState<ConfigPanelState>();
        var disabled = p.ShouldDisable();

        var container = new ContainerBuilder()
            .WithSection(new SectionBuilder()
                .WithTextDisplay($"## Configuration\n**Guild:** {state.GuildName}\n**Section:** {state.Section.Humanize()}"))
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (state.Section is ConfigSection.Moderation)
        {
            var scope = state.CategoryId is null ? "Global (Default)" : state.Categories.FirstOrDefault(c => c.Id == state.CategoryId)?.Name ?? "Unknown";
            container.WithTextDisplay($"### Moderation Rules\n**Scope:** {scope}");

            if (state.View is ConfigView.RoleSelect && state.PendingSettingKey is not null)
            {
                var title = state.PendingSettingKey == KeyMuteRole ? "Mute Role" : "Hard Mute Role";
                container
                    .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay($"### Set {title}\nPick a role below, or clear it.");
            }
            else
            {
                var body = string.Join("\n", new[]
                {
                    "### Filters & Expiry",
                    $"- Auto reprimand cooldown: {FormatLength(state.Moderation.AutoReprimandCooldown)}",
                    $"- Filtered expiry length: {FormatLength(state.Moderation.FilteredExpiryLength)}",
                    $"- Notice expiry length: {FormatLength(state.Moderation.NoticeExpiryLength)}",
                    $"- Warning expiry length: {FormatLength(state.Moderation.WarningExpiryLength)}",
                    $"- Censor expiry length: {FormatLength(state.Moderation.CensoredExpiryLength)}",
                    "",
                    "### Name Censoring",
                    $"- Censor nicknames: {OnOff(state.Moderation.CensorNicknames)}",
                    $"- Censor usernames: {OnOff(state.Moderation.CensorUsernames)}",
                    $"- Name replacement: {FormatOptional(state.Moderation.NameReplacement)}",
                    "",
                    "### Roles",
                    $"- Replace mutes: {OnOff(state.Moderation.ReplaceMutes)}",
                    $"- Mute role: {FormatRole(state.Moderation.MuteRoleId)}",
                    $"- Hard mute role: {FormatRole(state.Moderation.HardMuteRoleId)}"
                });

                container.WithTextDisplay(body.Truncate(3800));
            }
        }
        else
        {
            container.WithTextDisplay("-# This section will be upgraded to a full interactive editor next.");
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var sectionOptions = Enum.GetValues<ConfigSection>()
            .Select(s => new SelectMenuOptionBuilder(s.Humanize(), s.ToString(), isDefault: s == state.Section))
            .ToList();

        var sectionRow = new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(SectionSelectId)
                .WithPlaceholder("Switch section…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(sectionOptions)
                .WithDisabled(disabled));

        container.WithActionRow(sectionRow);

        if (state.Section is ConfigSection.Moderation)
        {
            var catOptions = new List<SelectMenuOptionBuilder>
            {
                new("Global (Default)", ConfigPanelState.GlobalCategoryValue, isDefault: state.CategoryId is null)
            };

            foreach (var category in state.Categories.Take(24))
            {
                catOptions.Add(new SelectMenuOptionBuilder(
                    category.Name.Truncate(100),
                    category.Id.ToString(),
                    isDefault: state.CategoryId == category.Id));
            }

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(CategorySelectId)
                    .WithPlaceholder("Change scope…")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithOptions(catOptions)
                    .WithDisabled(disabled)));

            if (state.View is ConfigView.RoleSelect)
            {
                container.WithActionRow(new ActionRowBuilder()
                    .WithSelectMenu(new SelectMenuBuilder()
                        .WithCustomId(RoleSelectId)
                        .WithPlaceholder("Select a role…")
                        .WithMinValues(1)
                        .WithMaxValues(1)
                        .WithType(ComponentType.RoleSelect)
                        .WithDisabled(disabled)));

                container.WithActionRow(new ActionRowBuilder()
                    .WithButton("Back", BackButtonId, ButtonStyle.Secondary, disabled: disabled)
                    .WithButton("Clear", ClearRoleId, ButtonStyle.Danger, disabled: disabled)
                    .WithButton($"Skip perms: {OnOff(state.SkipRolePermissionSetup)}",
                        ToggleSkipPermissionsId,
                        ButtonStyle.Secondary,
                        disabled: disabled));
            }
            else
            {
                var settings = new List<SelectMenuOptionBuilder>
                {
                    new("Toggle: Censor Nicknames", KeyCensorNicknames),
                    new("Toggle: Censor Usernames", KeyCensorUsernames),
                    new("Toggle: Replace Mutes", KeyReplaceMutes),
                    new("Set: Name Replacement", KeyNameReplacement),
                    new("Set: Auto Reprimand Cooldown", KeyAutoCooldown),
                    new("Set: Filtered Expiry Length", KeyFilteredExpiry),
                    new("Set: Notice Expiry Length", KeyNoticeExpiry),
                    new("Set: Warning Expiry Length", KeyWarningExpiry),
                    new("Set: Censor Expiry Length", KeyCensorExpiry),
                    new("Set: Mute Role", KeyMuteRole),
                    new("Set: Hard Mute Role", KeyHardMuteRole)
                };

                container.WithActionRow(new ActionRowBuilder()
                    .WithSelectMenu(new SelectMenuBuilder()
                        .WithCustomId(SettingSelectId)
                        .WithPlaceholder("Edit a setting…")
                        .WithMinValues(1)
                        .WithMaxValues(1)
                        .WithOptions(settings)
                        .WithDisabled(disabled)));

                container.WithActionRow(new ActionRowBuilder()
                    .WithButton("Refresh", RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
                    .AddStopButton(p));
            }
        }
        else
        {
            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Refresh", RefreshButtonId, ButtonStyle.Secondary, disabled: disabled)
                .AddStopButton(p));
        }

        if (state.LastUpdated is not null)
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay($"-# Updated {state.LastUpdated.Value.ToUniversalTimestamp()}")
                .WithAccentColor(AccentColor);
        }
        else
        {
            container.WithAccentColor(AccentColor);
        }

        return new PageBuilder()
            .WithComponents(new ComponentBuilderV2().WithContainer(container).Build())
            .Build();
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private async Task RespondWithTimeSpanModalAsync(
        IComponentInteraction interaction, ConfigPanelState state, string settingKey)
    {
        var title = SettingTitle(settingKey);
        var current = settingKey switch
        {
            KeyAutoCooldown => state.Moderation.AutoReprimandCooldown,
            KeyFilteredExpiry => state.Moderation.FilteredExpiryLength,
            KeyNoticeExpiry => state.Moderation.NoticeExpiryLength,
            KeyWarningExpiry => state.Moderation.WarningExpiryLength,
            KeyCensorExpiry => state.Moderation.CensoredExpiryLength,
            _ => null
        };

        await interaction.RespondWithModalAsync<ConfigTimeSpanModal>(
            $"{TimeSpanModalPrefix}:{settingKey}",
            modifyModal: m =>
            {
                m.WithTitle($"Set {title}");
                m.UpdateTextInput("value", x => x.Value = FormatTimeSpanInput(current));
            });
    }

    private async Task RespondWithStringModalAsync(
        IComponentInteraction interaction, ConfigPanelState state, string settingKey)
    {
        var title = SettingTitle(settingKey);
        var current = settingKey switch
        {
            KeyNameReplacement => state.Moderation.NameReplacement,
            _ => null
        };

        await interaction.RespondWithModalAsync<ConfigStringModal>(
            $"{StringModalPrefix}:{settingKey}",
            modifyModal: m =>
            {
                m.WithTitle($"Set {title}");
                m.UpdateTextInput("value", x => x.Value = current ?? string.Empty);
            });
    }

    private async Task<bool> TryApplyModerationToggleAsync(ConfigPanelState state, string settingKey)
    {
        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null) return false;

        switch (settingKey)
        {
            case KeyCensorNicknames:
                rules.CensorNicknames = !rules.CensorNicknames;
                return true;
            case KeyCensorUsernames:
                rules.CensorUsernames = !rules.CensorUsernames;
                return true;
            case KeyReplaceMutes:
                rules.ReplaceMutes = !rules.ReplaceMutes;
                return true;
            default:
                return false;
        }
    }

    private async Task<IModerationRules?> TryGetModerationRulesAsync(ConfigPanelState state)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);

        if (state.CategoryId is null)
            return guild.ModerationRules ??= new ModerationRules();

        return guild.ModerationCategories.FirstOrDefault(c => c.Id == state.CategoryId.Value);
    }

    private async Task SyncModerationSnapshotAsync(ConfigPanelState state)
    {
        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
            return;

        state.Moderation = ModerationRulesSnapshot.From(rules);
    }

    private static string SettingTitle(string key) => key switch
    {
        KeyCensorNicknames => "Censor Nicknames",
        KeyCensorUsernames => "Censor Usernames",
        KeyReplaceMutes => "Replace Mutes",
        KeyNameReplacement => "Name Replacement",
        KeyAutoCooldown => "Auto Reprimand Cooldown",
        KeyFilteredExpiry => "Filtered Expiry Length",
        KeyNoticeExpiry => "Notice Expiry Length",
        KeyWarningExpiry => "Warning Expiry Length",
        KeyCensorExpiry => "Censor Expiry Length",
        KeyMuteRole => "Mute Role",
        KeyHardMuteRole => "Hard Mute Role",
        _ => "Setting"
    };

    private static string OnOff(bool value) => value ? "ON" : "OFF";

    private static string FormatOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? "None" : $"`{value.Truncate(64)}`";

    private static string FormatRole(ulong? roleId)
        => roleId is null ? "None" : $"`{MentionUtils.MentionRole(roleId.Value)}`";

    private static string FormatLength(TimeSpan? length)
        => length is null ? "Disabled" : length.Value.Humanize(precision: 3);

    private static string FormatTimeSpanInput(TimeSpan? value)
    {
        if (value is null) return string.Empty;
        if (value.Value == TimeSpan.Zero) return "0s";

        var t = value.Value;
        var parts = new List<string>(4);
        if (t.Days != 0) parts.Add($"{t.Days}d");
        if (t.Hours != 0) parts.Add($"{t.Hours}h");
        if (t.Minutes != 0) parts.Add($"{t.Minutes}m");
        if (t.Seconds != 0) parts.Add($"{t.Seconds}s");
        return string.Join(string.Empty, parts);
    }

    private enum ConfigSection
    {
        Moderation,
        Logging,
        TimeTracking,
        VoiceChat
    }

    private enum ConfigView
    {
        Overview,
        RoleSelect
    }

    private sealed class ConfigPanelState
    {
        public const string GlobalCategoryValue = "global";

        public string GuildName { get; init; } = "Unknown";

        public IReadOnlyList<CategoryOption> Categories { get; init; } = Array.Empty<CategoryOption>();

        public Guid? CategoryId { get; set; }

        public ConfigSection Section { get; set; } = ConfigSection.Moderation;

        public ConfigView View { get; set; } = ConfigView.Overview;

        public string? PendingSettingKey { get; set; }

        public bool SkipRolePermissionSetup { get; set; }

        public ModerationRulesSnapshot Moderation { get; set; } = new();

        public DateTimeOffset? LastUpdated { get; set; }

        public static ConfigPanelState Create(GuildEntity guild, string guildName, ModerationCategory? category)
        {
            var categories = guild.ModerationCategories
                .Select(c => new CategoryOption(c.Id, c.Name))
                .OrderBy(c => c.Name)
                .ToList();

            Guid? categoryId = null;
            if (category is { Id: { } id } && id != Guid.Empty && categories.Any(c => c.Id == id))
                categoryId = id;

            var rules = categoryId is null
                ? (guild.ModerationRules ??= new ModerationRules()) as IModerationRules
                : guild.ModerationCategories.First(c => c.Id == categoryId);

            return new ConfigPanelState
            {
                GuildName = guildName,
                Categories = categories,
                CategoryId = categoryId,
                Moderation = ModerationRulesSnapshot.From(rules),
                LastUpdated = DateTimeOffset.UtcNow
            };
        }
    }

    private sealed record CategoryOption(Guid Id, string Name);

    private sealed class ModerationRulesSnapshot
    {
        public bool CensorNicknames { get; set; }
        public bool CensorUsernames { get; set; }
        public bool ReplaceMutes { get; set; }
        public string? NameReplacement { get; set; }
        public TimeSpan? AutoReprimandCooldown { get; set; }
        public TimeSpan? CensoredExpiryLength { get; set; }
        public TimeSpan? FilteredExpiryLength { get; set; }
        public TimeSpan? NoticeExpiryLength { get; set; }
        public TimeSpan? WarningExpiryLength { get; set; }
        public ulong? HardMuteRoleId { get; set; }
        public ulong? MuteRoleId { get; set; }

        public static ModerationRulesSnapshot From(IModerationRules rules) => new()
        {
            CensorNicknames = rules.CensorNicknames,
            CensorUsernames = rules.CensorUsernames,
            ReplaceMutes = rules.ReplaceMutes,
            NameReplacement = rules.NameReplacement,
            AutoReprimandCooldown = rules.AutoReprimandCooldown,
            CensoredExpiryLength = rules.CensoredExpiryLength,
            FilteredExpiryLength = rules.FilteredExpiryLength,
            NoticeExpiryLength = rules.NoticeExpiryLength,
            WarningExpiryLength = rules.WarningExpiryLength,
            HardMuteRoleId = rules.HardMuteRoleId,
            MuteRoleId = rules.MuteRoleId
        };
    }

    public class ConfigTimeSpanModal : IModal
    {
        public string Title => "Set TimeSpan";

        [RequiredInput(false)]
        [InputLabel("Value")]
        [ModalTextInput("value", TextInputStyle.Short, "Example: 1h30m (empty to disable)", maxLength: 100)]
        public TimeSpan? Value { get; set; }
    }

    public class ConfigStringModal : IModal
    {
        public string Title => "Set Value";

        [RequiredInput(false)]
        [InputLabel("Value")]
        [ModalTextInput("value", TextInputStyle.Paragraph, "Empty to clear", maxLength: 512)]
        public string? Value { get; set; }
    }
}

