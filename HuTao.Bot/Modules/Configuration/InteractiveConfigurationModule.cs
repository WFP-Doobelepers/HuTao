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
using Hangfire;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.TimeTracking;
using HuTao.Data.Models.VoiceChat;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Services.TimeTracking;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Configuration;

[RequireContext(ContextType.Guild)]
public class InteractiveConfigurationModule(
    HuTaoContext db,
    IMemoryCache cache,
    ModerationService moderation,
    GenshinTimeTrackingService timeTracking,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    private const string SectionSelectId = "cfg:section";
    private const string CategorySelectId = "cfg:category";
    private const string SettingSelectId = "cfg:setting";

    private const string OpenButtonId = "cfg:open";

    private const string OpenButtonId = "cfg:open";

    private const string BackButtonId = "cfg:back";
    private const string RefreshButtonId = "cfg:refresh";

    private const string RoleSelectId = "cfg:role";
    private const string ChannelSelectId = "cfg:channel";
    private const string ClearRoleId = "cfg:clear-role";
    private const string ClearChannelId = "cfg:clear-channel";
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

    private const string KeyLogUploadAttachments = "l:upload-attachments";
    private const string KeyLogEventPrefix = "l:event:";

    private const string KeyTimeServerStatus = "t:server-status";
    private const string KeyTimeAmerica = "t:america";
    private const string KeyTimeEurope = "t:europe";
    private const string KeyTimeAsia = "t:asia";
    private const string KeyTimeSar = "t:sar";

    private const string KeyVoiceHub = "v:hub";
    private const string KeyVoiceVoiceCategory = "v:voice-category";
    private const string KeyVoiceChatCategory = "v:chat-category";
    private const string KeyVoiceDeletionDelay = "v:deletion-delay";
    private const string KeyVoicePurgeEmpty = "v:purge-empty";
    private const string KeyVoiceShowJoinLeave = "v:show-join-leave";

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

    [ComponentInteraction(OpenButtonId, true)]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task OpenFromButtonAsync()
    {
        await DeferAsync(ephemeral: true);

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var state = ConfigPanelState.Create(guild, Context.Guild.Name, category: null);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(1)
            .WithUserState(state)
            .WithPageFactory(GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: true,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: InteractionResponseType.DeferredChannelMessageWithSource);
    }

    [ComponentInteraction(OpenButtonId, true)]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task OpenFromButtonAsync()
    {
        await DeferAsync(ephemeral: true);

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var state = ConfigPanelState.Create(guild, Context.Guild.Name, category: null);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(1)
            .WithUserState(state)
            .WithPageFactory(GeneratePage)
            .Build();

        await interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: true,
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

        state.PendingSettingKey = settingKey;

        switch (state.Section)
        {
            case ConfigSection.Moderation:
                await HandleModerationSettingAsync(paginator, state, interaction, settingKey);
                return;
            case ConfigSection.Logging:
                await HandleLoggingSettingAsync(paginator, state, interaction, settingKey);
                return;
            case ConfigSection.TimeTracking:
                await HandleTimeTrackingSettingAsync(paginator, state, interaction, settingKey);
                return;
            case ConfigSection.VoiceChat:
                await HandleVoiceChatSettingAsync(paginator, state, interaction, settingKey);
                return;
            default:
                await DeferAsync();
                await RenderAsync(paginator);
                return;
        }
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

        await SyncAllSnapshotsAsync(state);
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

    [ComponentInteraction(ChannelSelectId, true)]
    public async Task SelectChannelAsync(IGuildChannel[] channels)
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        if (state.View is not ConfigView.ChannelSelect || state.PendingSettingKey is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        var selected = channels.FirstOrDefault();
        if (selected is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        if (state.PendingSettingKey.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
        {
            var typeText = state.PendingSettingKey[KeyLogEventPrefix.Length..];
            if (Enum.TryParse<LogType>(typeText, ignoreCase: true, out var type))
                await SetLogEventChannelAsync(type, selected);
        }
        else
        {
            switch (state.PendingSettingKey)
            {
                case KeyTimeServerStatus:
                    await SetServerStatusAsync(selected);
                    break;
                case KeyTimeAmerica:
                    await SetRegionChannelAsync(selected, GenshinTimeTrackingService.ServerRegion.America);
                    break;
                case KeyTimeEurope:
                    await SetRegionChannelAsync(selected, GenshinTimeTrackingService.ServerRegion.Europe);
                    break;
                case KeyTimeAsia:
                    await SetRegionChannelAsync(selected, GenshinTimeTrackingService.ServerRegion.Asia);
                    break;
                case KeyTimeSar:
                    await SetRegionChannelAsync(selected, GenshinTimeTrackingService.ServerRegion.SAR);
                    break;
                case KeyVoiceHub:
                    if (selected is IVoiceChannel voice)
                        await SetVoiceHubAsync(voice);
                    break;
                case KeyVoiceVoiceCategory:
                    if (selected is ICategoryChannel voiceCategory)
                        await SetVoiceCategoriesAsync(voiceCategory, chatCategory: null);
                    break;
                case KeyVoiceChatCategory:
                    if (selected is ICategoryChannel chatCategory)
                        await SetVoiceCategoriesAsync(voiceCategory: null, chatCategory);
                    break;
            }
        }

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncAllSnapshotsAsync(state);

        await RenderAsync(paginator);
    }

    [ComponentInteraction(ClearChannelId, true)]
    public async Task ClearChannelAsync()
    {
        if (!TryGetPanelFromComponent(out var paginator, out var state, out _))
            return;

        if (state.View is not ConfigView.ChannelSelect || state.PendingSettingKey is null)
        {
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();

        if (state.PendingSettingKey.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
        {
            var typeText = state.PendingSettingKey[KeyLogEventPrefix.Length..];
            if (Enum.TryParse<LogType>(typeText, ignoreCase: true, out var type))
                await SetLogEventChannelAsync(type, channel: null);
        }
        else
        {
            switch (state.PendingSettingKey)
            {
                case KeyTimeServerStatus:
                    await ClearServerStatusAsync();
                    break;
                case KeyTimeAmerica:
                    await ClearRegionChannelAsync(GenshinTimeTrackingService.ServerRegion.America);
                    break;
                case KeyTimeEurope:
                    await ClearRegionChannelAsync(GenshinTimeTrackingService.ServerRegion.Europe);
                    break;
                case KeyTimeAsia:
                    await ClearRegionChannelAsync(GenshinTimeTrackingService.ServerRegion.Asia);
                    break;
                case KeyTimeSar:
                    await ClearRegionChannelAsync(GenshinTimeTrackingService.ServerRegion.SAR);
                    break;
                case KeyVoiceHub:
                    await ClearVoiceChatAsync();
                    break;
                case KeyVoiceVoiceCategory:
                    await ResetVoiceCategoriesToHubAsync(resetVoiceCategory: true, resetChatCategory: false);
                    break;
                case KeyVoiceChatCategory:
                    await ResetVoiceCategoriesToHubAsync(resetVoiceCategory: false, resetChatCategory: true);
                    break;
            }
        }

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SyncAllSnapshotsAsync(state);

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
        state.PendingSettingKey = null;
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
        state.PendingSettingKey = null;
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

        if (settingKey == KeyVoiceDeletionDelay)
        {
            var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.VoiceChatRules;
            if (rules is null)
            {
                state.View = ConfigView.Overview;
                state.PendingSettingKey = null;
                await SyncAllSnapshotsAsync(state);
                await RenderAsync(paginator);
                return;
            }

            var delay = modal.Value ?? TimeSpan.Zero;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            rules.DeletionDelay = delay;

            cache.InvalidateCaches(Context.Guild);
            await db.SaveChangesAsync();

            state.View = ConfigView.Overview;
            state.PendingSettingKey = null;
            state.LastUpdated = DateTimeOffset.UtcNow;
            await SyncVoiceChatSnapshotAsync(state);

            await RenderAsync(paginator);
            return;
        }

        var modRules = await TryGetModerationRulesAsync(state);
        if (modRules is null)
        {
            await RenderAsync(paginator);
            return;
        }

        switch (settingKey)
        {
            case KeyAutoCooldown:
                modRules.AutoReprimandCooldown = modal.Value;
                break;
            case KeyFilteredExpiry:
                modRules.FilteredExpiryLength = modal.Value;
                break;
            case KeyNoticeExpiry:
                modRules.NoticeExpiryLength = modal.Value;
                break;
            case KeyWarningExpiry:
                modRules.WarningExpiryLength = modal.Value;
                break;
            case KeyCensorExpiry:
                modRules.CensoredExpiryLength = modal.Value;
                break;
            default:
                await RenderAsync(paginator);
                return;
        }

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;
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
        state.PendingSettingKey = null;
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
        var lockSelectors = disabled || state.View is not ConfigView.Overview;

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Configuration\n**Guild:** {state.GuildName}\n**Section:** {state.Section.Humanize()}")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        switch (state.Section)
        {
            case ConfigSection.Moderation:
                RenderModeration();
                break;
            case ConfigSection.Logging:
                RenderLogging();
                break;
            case ConfigSection.TimeTracking:
                RenderTimeTracking();
                break;
            case ConfigSection.VoiceChat:
                RenderVoiceChat();
                break;
        }

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var sectionOptions = Enum.GetValues<ConfigSection>()
            .Select(s => new SelectMenuOptionBuilder(s.Humanize(), s.ToString(), isDefault: s == state.Section))
            .ToList();

        container.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId(SectionSelectId)
                .WithPlaceholder("Switch section…")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(sectionOptions)
                .WithDisabled(lockSelectors)));

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
                    .WithDisabled(lockSelectors)));
        }

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
        else if (state.View is ConfigView.ChannelSelect)
        {
            var (placeholder, channelTypes, allowClear) = GetChannelSelectConfig(state.PendingSettingKey);

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(ChannelSelectId)
                    .WithPlaceholder(placeholder)
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.ChannelSelect)
                    .WithChannelTypes(channelTypes)
                    .WithDisabled(disabled)));

            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Back", BackButtonId, ButtonStyle.Secondary, disabled: disabled)
                .WithButton("Clear", ClearChannelId, ButtonStyle.Danger, disabled: disabled || !allowClear));
        }
        else
        {
            var settings = BuildSettingOptions();
            if (settings.Count > 0)
            {
                container.WithActionRow(new ActionRowBuilder()
                    .WithSelectMenu(new SelectMenuBuilder()
                        .WithCustomId(SettingSelectId)
                        .WithPlaceholder("Edit a setting…")
                        .WithMinValues(1)
                        .WithMaxValues(1)
                        .WithOptions(settings)
                        .WithDisabled(disabled)));
            }

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

        void RenderModeration()
        {
            var scope = state.CategoryId is null
                ? "Global (Default)"
                : state.Categories.FirstOrDefault(c => c.Id == state.CategoryId)?.Name ?? "Unknown";

            container.WithTextDisplay($"### Moderation Rules\n**Scope:** {scope}");

            if (state.View is ConfigView.RoleSelect && state.PendingSettingKey is not null)
            {
                var title = state.PendingSettingKey == KeyMuteRole ? "Mute Role" : "Hard Mute Role";
                container
                    .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay($"### Set {title}\nPick a role below, or clear it.");
                return;
            }

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

        void RenderLogging()
        {
            if (state.View is ConfigView.ChannelSelect && state.PendingSettingKey is not null)
            {
                var title = SettingTitle(state.PendingSettingKey);
                var current = GetSelectedLogChannel(state.PendingSettingKey);
                container.WithTextDisplay(string.Join("\n", new[]
                {
                    $"### {title}",
                    $"**Current:** {FormatChannel(current)}",
                    "-# Select a channel below, or clear to disable this event."
                }).Truncate(2000));
                return;
            }

            var upload = state.Logging.UploadAttachments is { } u ? OnOff(u) : "Not configured";
            var lines = new List<string>
            {
                "### Message Logging",
                $"- Re-upload attachments: {upload}",
                "",
                "### Event Channels",
                $"- Message Updated: {FormatChannel(state.Logging.GetChannel(LogType.MessageUpdated))}",
                $"- Message Deleted: {FormatChannel(state.Logging.GetChannel(LogType.MessageDeleted))}",
                $"- Reaction Added: {FormatChannel(state.Logging.GetChannel(LogType.ReactionAdded))}",
                $"- Reaction Removed: {FormatChannel(state.Logging.GetChannel(LogType.ReactionRemoved))}",
                $"- Bulk Deleted: {FormatChannel(state.Logging.GetChannel(LogType.MessagesBulkDeleted))}"
            };

            container.WithTextDisplay(string.Join("\n", lines).Truncate(3800));
        }

        void RenderTimeTracking()
        {
            if (state.View is ConfigView.ChannelSelect && state.PendingSettingKey is not null)
            {
                var title = SettingTitle(state.PendingSettingKey);
                var current = state.PendingSettingKey switch
                {
                    KeyTimeServerStatus => state.TimeTracking.ServerStatusJumpUrl,
                    KeyTimeAmerica => FormatChannel(state.TimeTracking.AmericaChannelId),
                    KeyTimeEurope => FormatChannel(state.TimeTracking.EuropeChannelId),
                    KeyTimeAsia => FormatChannel(state.TimeTracking.AsiaChannelId),
                    KeyTimeSar => FormatChannel(state.TimeTracking.SarChannelId),
                    _ => null
                };

                container.WithTextDisplay(string.Join("\n", new[]
                {
                    $"### {title}",
                    $"**Current:** {FormatCurrent(current)}",
                    "-# Select a channel below, or clear to disable."
                }).Truncate(2000));
                return;
            }

            var serverStatus = state.TimeTracking.ServerStatusJumpUrl is { Length: > 0 } url ? $"[Jump]({url})" : "Not configured";
            var lines = new List<string>
            {
                "### Genshin Time Tracking",
                $"- Server status message: {serverStatus}",
                "",
                "### Region Channels (renamed every 5 minutes)",
                $"- America: {FormatChannel(state.TimeTracking.AmericaChannelId)}",
                $"- Europe: {FormatChannel(state.TimeTracking.EuropeChannelId)}",
                $"- Asia: {FormatChannel(state.TimeTracking.AsiaChannelId)}",
                $"- SAR: {FormatChannel(state.TimeTracking.SarChannelId)}"
            };

            container.WithTextDisplay(string.Join("\n", lines).Truncate(3800));
        }

        void RenderVoiceChat()
        {
            if (state.View is ConfigView.ChannelSelect && state.PendingSettingKey is not null)
            {
                var title = SettingTitle(state.PendingSettingKey);
                var current = state.PendingSettingKey switch
                {
                    KeyVoiceHub => FormatChannel(state.VoiceChat.HubVoiceChannelId),
                    KeyVoiceVoiceCategory => FormatChannel(state.VoiceChat.VoiceChannelCategoryId),
                    KeyVoiceChatCategory => FormatChannel(state.VoiceChat.VoiceChatCategoryId),
                    _ => null
                };

                container.WithTextDisplay(string.Join("\n", new[]
                {
                    $"### {title}",
                    $"**Current:** {FormatCurrent(current)}",
                    "-# Select a channel below. Hub must be inside a category."
                }).Truncate(2000));
                return;
            }

            if (!state.VoiceChat.Configured)
            {
                container.WithTextDisplay(string.Join("\n", new[]
                {
                    "### Voice Chat",
                    "Not configured.",
                    "-# Set a Hub Voice Channel to enable Voice Chat."
                }));
                return;
            }

            var lines = new List<string>
            {
                "### Voice Chat",
                $"- Hub voice channel: {FormatChannel(state.VoiceChat.HubVoiceChannelId)}",
                $"- Voice channel category: {FormatChannel(state.VoiceChat.VoiceChannelCategoryId)}",
                $"- Voice chat category: {FormatChannel(state.VoiceChat.VoiceChatCategoryId)}",
                $"- Deletion delay: {FormatLength(state.VoiceChat.DeletionDelay)}",
                $"- Purge empty: {OnOff(state.VoiceChat.PurgeEmpty ?? false)}",
                $"- Show join/leave: {OnOff(state.VoiceChat.ShowJoinLeave ?? false)}"
            };

            container.WithTextDisplay(string.Join("\n", lines).Truncate(3800));
        }

        List<SelectMenuOptionBuilder> BuildSettingOptions()
        {
            return state.Section switch
            {
                ConfigSection.Moderation => new List<SelectMenuOptionBuilder>
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
                },
                ConfigSection.Logging => new List<SelectMenuOptionBuilder>
                {
                    new("Toggle: Re-upload attachments", KeyLogUploadAttachments),
                    new("Set: Message Updated channel", $"{KeyLogEventPrefix}{LogType.MessageUpdated}"),
                    new("Set: Message Deleted channel", $"{KeyLogEventPrefix}{LogType.MessageDeleted}"),
                    new("Set: Reaction Added channel", $"{KeyLogEventPrefix}{LogType.ReactionAdded}"),
                    new("Set: Reaction Removed channel", $"{KeyLogEventPrefix}{LogType.ReactionRemoved}"),
                    new("Set: Bulk Deleted channel", $"{KeyLogEventPrefix}{LogType.MessagesBulkDeleted}")
                },
                ConfigSection.TimeTracking => new List<SelectMenuOptionBuilder>
                {
                    new("Set: Server Status message", KeyTimeServerStatus),
                    new("Set: America channel", KeyTimeAmerica),
                    new("Set: Europe channel", KeyTimeEurope),
                    new("Set: Asia channel", KeyTimeAsia),
                    new("Set: SAR channel", KeyTimeSar)
                },
                ConfigSection.VoiceChat when state.VoiceChat.Configured => new List<SelectMenuOptionBuilder>
                {
                    new("Set: Hub Voice Channel", KeyVoiceHub),
                    new("Set: Voice Channel Category", KeyVoiceVoiceCategory),
                    new("Set: Voice Chat Category", KeyVoiceChatCategory),
                    new("Set: Deletion Delay", KeyVoiceDeletionDelay),
                    new("Toggle: Purge Empty", KeyVoicePurgeEmpty),
                    new("Toggle: Show Join/Leave", KeyVoiceShowJoinLeave)
                },
                ConfigSection.VoiceChat => new List<SelectMenuOptionBuilder>
                {
                    new("Set: Hub Voice Channel", KeyVoiceHub)
                },
                _ => new List<SelectMenuOptionBuilder>()
            };
        }

        static (string Placeholder, ChannelType[] ChannelTypes, bool AllowClear) GetChannelSelectConfig(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return ("Select a channel…", [ChannelType.Text], false);

            if (key.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
                return ("Select a log channel…", [ChannelType.Text, ChannelType.News], true);

            return key switch
            {
                KeyTimeServerStatus => ("Select a channel for server status…", [ChannelType.Text, ChannelType.News], true),
                KeyTimeAmerica => ("Select a channel to rename (America)…", [ChannelType.Text, ChannelType.News], true),
                KeyTimeEurope => ("Select a channel to rename (Europe)…", [ChannelType.Text, ChannelType.News], true),
                KeyTimeAsia => ("Select a channel to rename (Asia)…", [ChannelType.Text, ChannelType.News], true),
                KeyTimeSar => ("Select a channel to rename (SAR)…", [ChannelType.Text, ChannelType.News], true),
                KeyVoiceHub => ("Select a hub voice channel…", [ChannelType.Voice], true),
                KeyVoiceVoiceCategory => ("Select a category…", [ChannelType.Category], true),
                KeyVoiceChatCategory => ("Select a category…", [ChannelType.Category], true),
                _ => ("Select a channel…", [ChannelType.Text], false)
            };
        }

        ulong? GetSelectedLogChannel(string key)
        {
            if (!key.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
                return null;

            var typeText = key[KeyLogEventPrefix.Length..];
            return Enum.TryParse<LogType>(typeText, ignoreCase: true, out var type)
                ? state.Logging.GetChannel(type)
                : null;
        }

        static string FormatChannel(ulong? channelId)
            => channelId is null ? "Disabled" : MentionUtils.MentionChannel(channelId.Value);

        static string FormatCurrent(string? current)
            => string.IsNullOrWhiteSpace(current) ? "Not configured" : current;
    }

    private async Task RenderAsync(IComponentPaginator paginator)
    {
        var page = await paginator.PageFactory(paginator);
        await paginator.RenderPageAsync(Context.Interaction, InteractionResponseType.DeferredUpdateMessage, false, page);
    }

    private async Task HandleModerationSettingAsync(
        IComponentPaginator paginator,
        ConfigPanelState state,
        IComponentInteraction interaction,
        string settingKey)
    {
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

        state.View = ConfigView.Overview;
        state.PendingSettingKey = null;
        await RenderAsync(paginator);
    }

    private async Task HandleLoggingSettingAsync(
        IComponentPaginator paginator,
        ConfigPanelState state,
        IComponentInteraction interaction,
        string settingKey)
    {
        _ = interaction;

        if (settingKey == KeyLogUploadAttachments)
        {
            await DeferAsync();

            var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
            guild.LoggingRules ??= new LoggingRules();
            guild.LoggingRules.UploadAttachments = !guild.LoggingRules.UploadAttachments;

            cache.InvalidateCaches(Context.Guild);
            await db.SaveChangesAsync();

            state.LastUpdated = DateTimeOffset.UtcNow;
            await SyncLoggingSnapshotAsync(state);

            state.View = ConfigView.Overview;
            state.PendingSettingKey = null;
            await RenderAsync(paginator);
            return;
        }

        if (settingKey.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
        {
            state.View = ConfigView.ChannelSelect;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();
        await RenderAsync(paginator);
    }

    private async Task HandleTimeTrackingSettingAsync(
        IComponentPaginator paginator,
        ConfigPanelState state,
        IComponentInteraction interaction,
        string settingKey)
    {
        _ = interaction;

        if (settingKey is KeyTimeServerStatus or KeyTimeAmerica or KeyTimeEurope or KeyTimeAsia or KeyTimeSar)
        {
            state.View = ConfigView.ChannelSelect;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();
        await RenderAsync(paginator);
    }

    private async Task HandleVoiceChatSettingAsync(
        IComponentPaginator paginator,
        ConfigPanelState state,
        IComponentInteraction interaction,
        string settingKey)
    {
        if (settingKey is KeyVoiceHub or KeyVoiceVoiceCategory or KeyVoiceChatCategory)
        {
            state.View = ConfigView.ChannelSelect;
            await DeferAsync();
            await RenderAsync(paginator);
            return;
        }

        if (settingKey is KeyVoiceDeletionDelay)
        {
            if (!state.VoiceChat.Configured)
            {
                await DeferAsync();
                await RenderAsync(paginator);
                return;
            }

            await RespondWithTimeSpanModalAsync(interaction, state, settingKey);
            return;
        }

        if (settingKey is KeyVoicePurgeEmpty or KeyVoiceShowJoinLeave)
        {
            if (!state.VoiceChat.Configured)
            {
                await DeferAsync();
                await RenderAsync(paginator);
                return;
            }

            await DeferAsync();

            var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
            var rules = guild.VoiceChatRules;
            if (rules is null)
            {
                await RenderAsync(paginator);
                return;
            }

            switch (settingKey)
            {
                case KeyVoicePurgeEmpty:
                    rules.PurgeEmpty = !rules.PurgeEmpty;
                    break;
                case KeyVoiceShowJoinLeave:
                    rules.ShowJoinLeave = !rules.ShowJoinLeave;
                    break;
            }

            cache.InvalidateCaches(Context.Guild);
            await db.SaveChangesAsync();

            state.LastUpdated = DateTimeOffset.UtcNow;
            await SyncVoiceChatSnapshotAsync(state);

            state.View = ConfigView.Overview;
            state.PendingSettingKey = null;
            await RenderAsync(paginator);
            return;
        }

        await DeferAsync();
        await RenderAsync(paginator);
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
            KeyVoiceDeletionDelay => state.VoiceChat.DeletionDelay,
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

    private async Task SetLogEventChannelAsync(LogType type, IGuildChannel? channel)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();

        var rules = guild.LoggingRules.LoggingChannels;
        var existing = rules.Where(r => r.Type == type).ToList();
        foreach (var rule in existing)
        {
            rules.Remove(rule);
            db.Remove(rule);
        }

        if (channel is ITextChannel)
            rules.Add(new EnumChannel<LogType>(type, channel));

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task SetServerStatusAsync(IGuildChannel channel)
    {
        if (channel is not ITextChannel textChannel)
            return;

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.GenshinRules ??= new GenshinTimeTrackingRules();

        var rules = guild.GenshinRules;
        if (rules.ServerStatus is not null)
        {
            var removed = db.Remove(rules.ServerStatus).Entity;
            RecurringJob.RemoveIfExists(removed.Id.ToString());
        }

        var placeholder = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay("## Server Status\nSetting up…")
                .WithAccentColor(AccentColor))
            .Build();

        var message = await textChannel.SendMessageAsync(components: placeholder, allowedMentions: AllowedMentions.None);

        rules.ServerStatus = new MessageTimeTracking
        {
            GuildId = guild.Id,
            ChannelId = message.Channel.Id,
            MessageId = message.Id
        };

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        await timeTracking.TrackGenshinTime(guild);
        await timeTracking.UpdateMessageAsync(guild.Id, message.Channel.Id, message.Id);
    }

    private async Task ClearServerStatusAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.GenshinRules;
        if (rules?.ServerStatus is null)
            return;

        var removed = db.Remove(rules.ServerStatus).Entity;
        RecurringJob.RemoveIfExists(removed.Id.ToString());

        rules.ServerStatus = null;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task SetRegionChannelAsync(IGuildChannel channel, GenshinTimeTrackingService.ServerRegion region)
    {
        if (channel is not ITextChannel)
            return;

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.GenshinRules ??= new GenshinTimeTrackingRules();

        var rules = guild.GenshinRules;
        var current = region switch
        {
            GenshinTimeTrackingService.ServerRegion.America => rules.AmericaChannel,
            GenshinTimeTrackingService.ServerRegion.Europe => rules.EuropeChannel,
            GenshinTimeTrackingService.ServerRegion.Asia => rules.AsiaChannel,
            GenshinTimeTrackingService.ServerRegion.SAR => rules.SARChannel,
            _ => null
        };

        if (current is not null)
        {
            RecurringJob.RemoveIfExists(current.Id.ToString());
            db.Remove(current);
        }

        var tracking = new ChannelTimeTracking
        {
            GuildId = guild.Id,
            ChannelId = channel.Id
        };

        switch (region)
        {
            case GenshinTimeTrackingService.ServerRegion.America:
                rules.AmericaChannel = tracking;
                break;
            case GenshinTimeTrackingService.ServerRegion.Europe:
                rules.EuropeChannel = tracking;
                break;
            case GenshinTimeTrackingService.ServerRegion.Asia:
                rules.AsiaChannel = tracking;
                break;
            case GenshinTimeTrackingService.ServerRegion.SAR:
                rules.SARChannel = tracking;
                break;
        }

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();

        await timeTracking.TrackGenshinTime(guild);
        await timeTracking.UpdateChannelAsync(guild.Id, channel.Id, region);
    }

    private async Task ClearRegionChannelAsync(GenshinTimeTrackingService.ServerRegion region)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.GenshinRules;
        if (rules is null)
            return;

        ChannelTimeTracking? current;
        switch (region)
        {
            case GenshinTimeTrackingService.ServerRegion.America:
                current = rules.AmericaChannel;
                rules.AmericaChannel = null;
                break;
            case GenshinTimeTrackingService.ServerRegion.Europe:
                current = rules.EuropeChannel;
                rules.EuropeChannel = null;
                break;
            case GenshinTimeTrackingService.ServerRegion.Asia:
                current = rules.AsiaChannel;
                rules.AsiaChannel = null;
                break;
            case GenshinTimeTrackingService.ServerRegion.SAR:
                current = rules.SARChannel;
                rules.SARChannel = null;
                break;
            default:
                return;
        }

        if (current is null)
            return;

        RecurringJob.RemoveIfExists(current.Id.ToString());
        db.Remove(current);

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task SetVoiceHubAsync(IVoiceChannel hubVoiceChannel)
    {
        if (hubVoiceChannel.CategoryId is null)
            return;

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.VoiceChatRules ??= new VoiceChatRules
        {
            GuildId = guild.Id,
            HubVoiceChannelId = hubVoiceChannel.Id,
            VoiceChannelCategoryId = hubVoiceChannel.CategoryId.Value,
            VoiceChatCategoryId = hubVoiceChannel.CategoryId.Value,
            DeletionDelay = TimeSpan.Zero,
            PurgeEmpty = true,
            ShowJoinLeave = true
        };

        if (guild.VoiceChatRules.HubVoiceChannelId != hubVoiceChannel.Id)
            guild.VoiceChatRules.HubVoiceChannelId = hubVoiceChannel.Id;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task SetVoiceCategoriesAsync(ICategoryChannel? voiceCategory, ICategoryChannel? chatCategory)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.VoiceChatRules;
        if (rules is null)
            return;

        if (voiceCategory is not null)
            rules.VoiceChannelCategoryId = voiceCategory.Id;

        if (chatCategory is not null)
            rules.VoiceChatCategoryId = chatCategory.Id;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task ClearVoiceChatAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        if (guild.VoiceChatRules is null)
            return;

        db.Remove(guild.VoiceChatRules);
        guild.VoiceChatRules = null;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task ResetVoiceCategoriesToHubAsync(bool resetVoiceCategory, bool resetChatCategory)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.VoiceChatRules;
        if (rules is null)
            return;

        var hub = await ((IGuild)Context.Guild).GetChannelAsync(rules.HubVoiceChannelId);
        if (hub is not IVoiceChannel { CategoryId: { } categoryId })
            return;

        if (resetVoiceCategory)
            rules.VoiceChannelCategoryId = categoryId;
        if (resetChatCategory)
            rules.VoiceChatCategoryId = categoryId;

        cache.InvalidateCaches(Context.Guild);
        await db.SaveChangesAsync();
    }

    private async Task SyncModerationSnapshotAsync(ConfigPanelState state)
    {
        var rules = await TryGetModerationRulesAsync(state);
        if (rules is null)
            return;

        state.Moderation = ModerationRulesSnapshot.From(rules);
    }

    private async Task SyncLoggingSnapshotAsync(ConfigPanelState state)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        state.Logging = LoggingRulesSnapshot.From(guild.LoggingRules);
    }

    private async Task SyncTimeTrackingSnapshotAsync(ConfigPanelState state)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        state.TimeTracking = TimeTrackingSnapshot.From(guild.GenshinRules);
    }

    private async Task SyncVoiceChatSnapshotAsync(ConfigPanelState state)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        state.VoiceChat = VoiceChatSnapshot.From(guild.VoiceChatRules);
    }

    private async Task SyncAllSnapshotsAsync(ConfigPanelState state)
    {
        await SyncModerationSnapshotAsync(state);
        await SyncLoggingSnapshotAsync(state);
        await SyncTimeTrackingSnapshotAsync(state);
        await SyncVoiceChatSnapshotAsync(state);
    }

    private static string SettingTitle(string key)
    {
        if (key.StartsWith(KeyLogEventPrefix, StringComparison.Ordinal))
        {
            var typeText = key[KeyLogEventPrefix.Length..];
            return Enum.TryParse<LogType>(typeText, ignoreCase: true, out var type)
                ? $"Log Event: {type.Humanize(LetterCasing.Title)}"
                : "Log Event";
        }

        return key switch
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
            KeyLogUploadAttachments => "Re-upload attachments",
            KeyTimeServerStatus => "Genshin: Server Status",
            KeyTimeAmerica => "Genshin: America Channel",
            KeyTimeEurope => "Genshin: Europe Channel",
            KeyTimeAsia => "Genshin: Asia Channel",
            KeyTimeSar => "Genshin: SAR Channel",
            KeyVoiceHub => "Voice Chat: Hub Voice Channel",
            KeyVoiceVoiceCategory => "Voice Chat: Voice Channel Category",
            KeyVoiceChatCategory => "Voice Chat: Voice Chat Category",
            KeyVoiceDeletionDelay => "Voice Chat: Deletion Delay",
            KeyVoicePurgeEmpty => "Voice Chat: Purge Empty",
            KeyVoiceShowJoinLeave => "Voice Chat: Show Join/Leave",
            _ => "Setting"
        };
    }

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
        RoleSelect,
        ChannelSelect
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

        public LoggingRulesSnapshot Logging { get; set; } = new();

        public TimeTrackingSnapshot TimeTracking { get; set; } = new();

        public VoiceChatSnapshot VoiceChat { get; set; } = new();

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
                Logging = LoggingRulesSnapshot.From(guild.LoggingRules),
                TimeTracking = TimeTrackingSnapshot.From(guild.GenshinRules),
                VoiceChat = VoiceChatSnapshot.From(guild.VoiceChatRules),
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

    private sealed class LoggingRulesSnapshot
    {
        public bool? UploadAttachments { get; set; }

        public Dictionary<LogType, ulong> LogChannels { get; } = new();

        public static LoggingRulesSnapshot From(LoggingRules? rules)
        {
            var snapshot = new LoggingRulesSnapshot
            {
                UploadAttachments = rules?.UploadAttachments
            };

            if (rules is null)
                return snapshot;

            foreach (var channel in rules.LoggingChannels)
                snapshot.LogChannels.TryAdd(channel.Type, channel.ChannelId);

            return snapshot;
        }

        public ulong? GetChannel(LogType type)
            => LogChannels.TryGetValue(type, out var id) ? id : null;
    }

    private sealed class TimeTrackingSnapshot
    {
        public bool Configured { get; set; }

        public string? ServerStatusJumpUrl { get; set; }

        public ulong? AmericaChannelId { get; set; }
        public ulong? EuropeChannelId { get; set; }
        public ulong? AsiaChannelId { get; set; }
        public ulong? SarChannelId { get; set; }

        public static TimeTrackingSnapshot From(GenshinTimeTrackingRules? rules) => new()
        {
            Configured = rules is not null,
            ServerStatusJumpUrl = rules?.ServerStatus?.JumpUrl,
            AmericaChannelId = rules?.AmericaChannel?.ChannelId,
            EuropeChannelId = rules?.EuropeChannel?.ChannelId,
            AsiaChannelId = rules?.AsiaChannel?.ChannelId,
            SarChannelId = rules?.SARChannel?.ChannelId
        };
    }

    private sealed class VoiceChatSnapshot
    {
        public bool Configured { get; set; }

        public ulong? HubVoiceChannelId { get; set; }

        public ulong? VoiceChannelCategoryId { get; set; }

        public ulong? VoiceChatCategoryId { get; set; }

        public TimeSpan? DeletionDelay { get; set; }

        public bool? PurgeEmpty { get; set; }

        public bool? ShowJoinLeave { get; set; }

        public static VoiceChatSnapshot From(VoiceChatRules? rules) => new()
        {
            Configured = rules is not null,
            HubVoiceChannelId = rules?.HubVoiceChannelId,
            VoiceChannelCategoryId = rules?.VoiceChannelCategoryId,
            VoiceChatCategoryId = rules?.VoiceChatCategoryId,
            DeletionDelay = rules?.DeletionDelay,
            PurgeEmpty = rules?.PurgeEmpty,
            ShowJoinLeave = rules?.ShowJoinLeave
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

