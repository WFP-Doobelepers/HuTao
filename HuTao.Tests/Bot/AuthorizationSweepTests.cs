using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;
using BotEntry = HuTao.Bot.Bot;
using CommandAttribute = Discord.Commands.CommandAttribute;
using CommandsPrecondition = Discord.Commands.PreconditionAttribute;
using IModuleBase = Discord.Commands.IModuleBase;
using InteractionsPrecondition = Discord.Interactions.PreconditionAttribute;

namespace HuTao.Tests.Bot;

// Runs the authorization preconditions of every text command and every interaction handler as a member with no scopes
// and no Discord permissions. Each must refuse, unless the allow-list below names it with the reason it may stay open.
public class AuthorizationSweepTests(ITestOutputHelper output)
{
    private const ulong MemberId = 41;
    private const ulong OwnerOfTheBot = 999;

    /// <summary>Handlers that a member with no scopes may run, keyed by kind, module and method, with the reason.</summary>
    private static readonly Dictionary<string, string> OpenOnPurpose = Open(
        ("Read-only display for everyone.",
        [
            "interaction:GenshinInteractive.CodesAsync", "interaction:GenshinInteractive.CodesListAsync",
            "interaction:GenshinInteractive.LivestreamAsync", "interaction:GenshinInteractive.VersionsAsync",
            "interaction:TheorycraftingModule.CritCompareAsync", "interaction:TheorycraftingModule.FavoniusAsync",
            "text:GeneralModule.PingAsync", "text:GenshinCommands.LivestreamAsync",
            "text:GenshinCommands.VersionsCommand"
        ]),
        ("The codes list posts to another channel only with Send Messages and Manage Webhooks there, or "
        + "the Genshin allowed list, and with a 10-second cooldown.",
        [
            "text:GenshinCommands.CodesAsync"
        ]),
        ("Help and the command list, filtered to what the member may run, and bound to the session of "
        + "the member who opened them.",
        [
            "interaction:InteractiveHelpModule.BackAsync", "interaction:InteractiveHelpModule.HelpAsync",
            "interaction:InteractiveHelpModule.SearchAsync", "interaction:InteractiveHelpModule.SearchModalAsync",
            "interaction:InteractiveHelpModule.SelectCommandAsync",
            "interaction:InteractiveHelpModule.SelectModuleAsync",
            "interaction:InteractiveHelpModule.SelectTagAsync", "interaction:SlashCommandsModule.BackAsync",
            "interaction:SlashCommandsModule.CommandsAsync", "interaction:SlashCommandsModule.SelectCommandAsync",
            "text:HelpModule.HelpAsync", "text:HelpModule.HelpCommandAsync", "text:HelpModule.HelpDMAsync",
            "text:HelpModule.HelpModuleAsync"
        ]),
        ("Read-only role browser, bound to the session of the member who opened it.",
        [
            "interaction:InteractiveRolesModule.BackAsync", "interaction:InteractiveRolesModule.ClearFilterAsync",
            "interaction:InteractiveRolesModule.OpenFromButtonAsync",
            "interaction:InteractiveRolesModule.RefreshAsync", "interaction:InteractiveRolesModule.RolesAsync",
            "interaction:InteractiveRolesModule.SearchAsync", "interaction:InteractiveRolesModule.SearchModalAsync",
            "interaction:InteractiveRolesModule.SelectRoleAsync"
        ]),
        ("Moderation panel component: bound to the session of the moderator who opened it, or checks the "
        + "scope in the handler.",
        [
            "interaction:ModerationInteractionHandlers.HandleAutoDeleteAsync",
            "interaction:ModerationInteractionHandlers.HandleAutoToggleAsync",
            "interaction:ModerationInteractionHandlers.HandleHistoryCategoryFilterAsync",
            "interaction:ModerationInteractionHandlers.HandleHistoryToggleImagesAsync",
            "interaction:ModerationInteractionHandlers.HandleHistoryTypeFilterAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteCategoryFilterAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteDetailsAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteExtendAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteExtendModalAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteRefreshAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteUnmuteAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutCategoryFilterAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutDetailsAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutRefreshAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutRemoveAsync"
        ]),
        ("Voice chat control: the handler checks that the caller owns the voice chat or can manage it.",
        [
            "interaction:InteractiveVoiceChatModule.BanAsync",
            "interaction:InteractiveVoiceChatModule.OpenFromButtonAsync",
            "interaction:InteractiveVoiceChatModule.PanelAsync",
            "interaction:InteractiveVoiceChatModule.RefreshAsync",
            "interaction:InteractiveVoiceChatModule.SetLimitAsync",
            "interaction:InteractiveVoiceChatModule.SetLimitModalAsync",
            "interaction:InteractiveVoiceChatModule.ToggleHideAsync",
            "interaction:InteractiveVoiceChatModule.ToggleLockAsync",
            "interaction:InteractiveVoiceChatModule.TransferAsync",
            "interaction:InteractiveVoiceChatModule.UnbanAsync", "text:VoiceChatModule.BanAsync",
            "text:VoiceChatModule.ClaimAsync", "text:VoiceChatModule.HideAsync", "text:VoiceChatModule.KickAsync",
            "text:VoiceChatModule.LimitAsync", "text:VoiceChatModule.LockAsync", "text:VoiceChatModule.OwnerAsync",
            "text:VoiceChatModule.RevealAsync", "text:VoiceChatModule.TransferAsync",
            "text:VoiceChatModule.UnbanAsync", "text:VoiceChatModule.UnlockAsync"
        ]),
        ("Quote buttons: Expand, Show all and Context check that the member can read the source channel, "
        + "and Collapse shows less than the posted quote.",
        [
            "interaction:InteractiveQuoteModule.CollapseQuoteAsync",
            "interaction:InteractiveQuoteModule.ExpandQuoteAsync", "interaction:InteractiveQuoteModule.ShowAllAsync"
        ]),
        ("Deleting a message checks Manage Messages or the Purge scope in the handler.",
        [
            "interaction:InteractiveGeneralModule.DeleteMessageAsync"
        ]),
        ("Applying a template checks the template's own scope in the handler.",
        [
            "interaction:InteractiveModerationModule.TemplateAsync", "text:ModerationModule.TemplateAsync"
        ]),
        ("A linked button: any member who sees it may press it, it must belong to this server, and its "
        + "roles follow the saved role rule.",
        [
            "interaction:InteractiveMessagesModule.ViewTemplateAsync"
        ]),
        ("Liben self-service: each handler acts only on the caller's own listing or looking status.",
        [
            "interaction:LibenModule.AddAsync", "interaction:LibenModule.AddInteractiveAsync",
            "interaction:LibenModule.AddModalAsync", "interaction:LibenModule.ListBoxesAsync",
            "interaction:LibenModule.LookingAsync", "interaction:LibenModule.LookingInteractiveAsync",
            "interaction:LibenModule.NotLookingAsync", "interaction:LibenModule.RemoveAsync"
        ]));

    private static Dictionary<string, string> Open(params (string Reason, string[] Handlers)[] groups)
        => groups.SelectMany(g => g.Handlers.Select(h => (Handler: h, g.Reason))).ToDictionary(x => x.Handler, x => x.Reason);

    // The precondition types that decide who may run a handler. Others, such as the server-only context, decide where.
    // CheckCategory guards the category parameter, and with no category given it checks the member's default one.
    private static readonly string[] AuthorizationKinds =
    [
        "RequireAuthorizationAttribute", "RequireCategoryAuthorizationAttribute",
        "RequireAnyCategoryAuthorizationAttribute", "RequireTeamMemberAttribute", "RequireOwnerAttribute",
        "RequireUserPermissionAttribute", "RequireAllowedUserCommandAttribute", "RequireAllowedUserInteractionAttribute",
        "CheckCategoryAttribute"
    ];

    /// <summary>
    ///     Member and role targets that a moderator who ranks below the target may still name, keyed by handler and
    ///     parameter, with the reason.
    /// </summary>
    private static readonly Dictionary<string, string> TargetsOpenOnPurpose = Open(
        ("Read-only: it shows the member, role or permissions and changes nothing.",
        [
            "interaction:InteractiveRolesModule.SelectRoleAsync(roles)",
            "interaction:InteractiveUserModule.ComponentHistoryAsync(user)",
            "interaction:InteractiveUserModule.ComponentInformationAsync(user)",
            "interaction:InteractiveUserModule.SlashAvatarAsync(user)",
            "interaction:InteractiveUserModule.SlashHistoryAsync(user)",
            "interaction:InteractiveUserModule.SlashInformationAsync(user)",
            "interaction:InteractiveUserModule.SlashWhoIsAsync(user)",
            "interaction:InteractiveUserModule.UserAvatarAsync(user)",
            "interaction:InteractiveUserModule.UserHistoryAsync(user)",
            "interaction:InteractiveUserModule.UserInformationAsync(user)", "text:RoleModule.ViewRoleAsync(role)",
            "text:RoleModule.ViewRolesAsync(roles)", "text:UserModule.AvatarAsync(user)",
            "text:UserModule.InfractionsAsync(user)", "text:UserModule.UserAsync(user)"
        ]),
        ("Opens the reprimand form only, and the form's own handler checks the moderator's rank.",
        [
            "interaction:InteractiveModerationModule.RespondModMenuAsync(user)"
        ]),
        ("A banned user is not in the server, so there is no rank to compare.",
        [
            "interaction:InteractiveModerationModule.UnbanAsync(user)", "text:ModerationModule.UnbanAsync(user)",
            "text:ModerationModule.UnbanAsync(users)"
        ]),
        ("The handler refuses a rule for a member or role that ranks at or above the moderator.",
        [
            "interaction:InteractiveCategoryPermissionsModule.AddRolePermissionAsync(role)",
            "interaction:InteractiveCategoryPermissionsModule.AddUserPermissionAsync(user)",
            "interaction:InteractivePermissionsModule.AddRolePermissionAsync(role)",
            "interaction:InteractivePermissionsModule.AddUserPermissionAsync(user)"
        ]),
        ("The handler checks the chosen role against the moderator's rank.",
        [
            "interaction:InteractiveConfigurationModule.SelectRoleAsync(roles)",
            "text:ConfigureModule.ConfigureHardMuteAsync(role)", "text:ConfigureModule.ConfigureMuteAsync(role)"
        ]),
        ("Discord lets a member with Manage Roles give or take any role below their own, whatever the "
        + "member's rank, and the role parameter checks that rank.",
        [
            "text:AuthorizedRoleModule.AddRoleAsync(user)", "text:AuthorizedRoleModule.AddRolesAsync(user)",
            "text:AuthorizedRoleModule.AddTemporaryRoleMemberAsync(user)",
            "text:AuthorizedRoleModule.RemoveRolesAsync(user)", "text:AuthorizedRoleModule.ToggleRoleAsync(user)"
        ]),
        ("Discord lets a member with Manage Messages delete any member's messages, whatever their rank.",
        [
            "interaction:InteractivePurgeModule.PurgeAsync(user)"
        ]),
        ("An exclusion only exempts the member or role from a filter or from logging.",
        [
            "interaction:InteractiveCensorExclusionsModule.ExcludeRoleAsync(role)",
            "interaction:InteractiveCensorExclusionsModule.ExcludeUserAsync(user)",
            "interaction:InteractiveLoggingExclusionsModule.AddRoleAsync(roles)",
            "interaction:InteractiveLoggingExclusionsModule.AddUserAsync(users)",
            "interaction:InteractiveModerationExclusionsModule.ExcludeRoleAsync(role)",
            "interaction:InteractiveModerationExclusionsModule.ExcludeUserAsync(user)"
        ]),
        ("The Genshin allowed list only lets a member post the codes list, and gives no rank or scope.",
        [
            "interaction:GenshinInteractive.AddAllowedRoleAsync(roles)",
            "interaction:GenshinInteractive.AddAllowedUserAsync(users)",
            "interaction:GenshinInteractive.AllowAsync(role)", "interaction:GenshinInteractive.AllowAsync(user)",
            "interaction:GenshinInteractive.DisallowAsync(role)",
            "interaction:GenshinInteractive.DisallowAsync(user)",
            "interaction:GenshinInteractive.RemoveAllowedRoleAsync(roles)",
            "interaction:GenshinInteractive.RemoveAllowedUserAsync(users)", "text:GenshinCommands.AllowRole(role)",
            "text:GenshinCommands.AllowUser(user)", "text:GenshinCommands.DisallowRole(role)",
            "text:GenshinCommands.DisallowUser(user)"
        ]),
        ("Voice chat: kick and ban check the owner's rank in the handler, a transfer gives the voice "
        + "chat away, and an unban gives access back.",
        [
            "interaction:InteractiveVoiceChatModule.BanAsync(users)",
            "interaction:InteractiveVoiceChatModule.TransferAsync(users)",
            "interaction:InteractiveVoiceChatModule.UnbanAsync(users)", "text:VoiceChatModule.BanAsync(user)",
            "text:VoiceChatModule.KickAsync(user)", "text:VoiceChatModule.TransferAsync(user)",
            "text:VoiceChatModule.UnbanAsync(user)"
        ]),
        ("Liben listings belong to the Liben feature and carry no rank.",
        [
            "interaction:LibenModule.ForceRemoveBoxAsync(user)"
        ]));

    /// <summary>
    ///     Handlers that a member with every scope in one category passes without naming a category, keyed by kind, module
    ///     and method, with the reason they act only within what that member may do.
    /// </summary>
    private static readonly Dictionary<string, string> OpenToAnotherCategory = Open(
        ("Opens the reprimand form only, and the form's handler checks the category the moderator picks.",
        [
            "interaction:InteractiveModerationModule.BanMenuAsync",
            "interaction:InteractiveModerationModule.HardMuteMenuAsync",
            "interaction:InteractiveModerationModule.KickMenuAsync",
            "interaction:InteractiveModerationModule.MuteMenuAsync",
            "interaction:InteractiveModerationModule.NoteMenuAsync",
            "interaction:InteractiveModerationModule.NoticeMenuAsync",
            "interaction:InteractiveModerationModule.RespondModMenuAsync",
            "interaction:InteractiveModerationModule.TimeoutMenuAsync",
            "interaction:InteractiveModerationModule.WarnMenuAsync"
        ]),
        ("History opens on a category the member may view, and its filter offers only those categories.",
        [
            "interaction:InteractiveReprimandsModule.ViewUserHistoryAsync",
            "interaction:InteractiveUserModule.ComponentHistoryAsync",
            "interaction:InteractiveUserModule.UserHistoryAsync"
        ]),
        ("The handler checks the category of the reprimand it acts on.",
        [
            "interaction:InteractiveReprimandsModule.PardonReprimandAsync",
            "interaction:InteractiveReprimandsModule.PardonReprimandMenuAsync",
            "interaction:InteractiveReprimandsModule.RemoveEntityAsync",
            "interaction:InteractiveReprimandsModule.UpdateReprimandAsync",
            "interaction:InteractiveReprimandsModule.UpdateReprimandMenuAsync",
            "interaction:InteractiveReprimandsModule.ViewReprimandAsync",
            "text:ModifyReprimandsModule.PardonReprimandAsync", "text:ModifyReprimandsModule.RemoveEntityAsync",
            "text:ModifyReprimandsModule.UpdateReprimandAsync", "text:ModifyReprimandsModule.ViewReprimandAsync"
        ]));

    /// <summary>
    ///     Handlers that take an ID typed in or carried in a custom ID, keyed by kind, module and method, with how each
    ///     one keeps the ID to this server's records.
    /// </summary>
    private static readonly Dictionary<string, string> IdsCheckedAgainstThisServer = Open(
        ("Finds the ID only in this server's own list, the one the command shows.",
        [
            "interaction:InteractiveAutoModerationModule.DeleteAsync",
            "interaction:InteractiveAutoModerationModule.ToggleAsync",
            "interaction:InteractiveCategoriesModule.RemoveEntityAsync",
            "interaction:InteractiveCategoryPermissionsModule.RemovePermissionAsync",
            "interaction:InteractiveCensorExclusionsModule.RemoveEntityAsync",
            "interaction:InteractiveCensorsModule.RemoveEntityAsync",
            "interaction:InteractiveLinkedButtonModule.RemoveEntityAsync",
            "interaction:InteractiveLinkedCommandModule.RemoveEntityAsync",
            "interaction:InteractiveLoggingExclusionsModule.RemoveAsync",
            "interaction:InteractiveModerationExclusionsModule.RemoveExclusionAsync",
            "interaction:InteractivePermissionsModule.RemoveEntityAsync",
            "interaction:InteractiveStickyModule.DisableAsync", "interaction:InteractiveStickyModule.EnableAsync",
            "interaction:InteractiveStickyModule.RemoveEntityAsync",
            "interaction:InteractiveTemplatesModule.RemoveEntityAsync",
            "interaction:InteractiveVariablesModule.RemoveEntityAsync",
            "text:AutoModerationModule.DeleteTriggerAsync", "text:AutoModerationModule.EnableEntityAsync",
            "text:AutoModerationModule.RemoveEntityAsync", "text:AutoModerationModule.ToggleEntityAsync",
            "text:CategoryPermissionsModule.RemoveEntityAsync", "text:CensorExclusionsModule.RemoveEntityAsync",
            "text:CensorModule.DeleteTriggerAsync", "text:CensorModule.EnableEntityAsync",
            "text:CensorModule.RemoveEntityAsync", "text:CensorModule.ToggleEntityAsync",
            "text:LinkedButtonModule.RemoveEntityAsync", "text:LinkedCommandModule.RemoveEntityAsync",
            "text:LoggingExclusionsModule.RemoveEntityAsync", "text:MessageTemplateModule.LinkAsync",
            "text:MessageTemplateModule.RemoveEntityAsync", "text:ModerationCategoryModule.RemoveEntityAsync",
            "text:ModerationExclusionsModule.RemoveEntityAsync", "text:ModerationTemplatesModule.RemoveEntityAsync",
            "text:ModerationVariablesModule.RemoveEntityAsync", "text:PermissionsModule.RemoveEntityAsync",
            "text:ReprimandTriggersModule.DeleteTriggerAsync", "text:ReprimandTriggersModule.EnableEntityAsync",
            "text:ReprimandTriggersModule.RemoveEntityAsync", "text:ReprimandTriggersModule.ToggleEntityAsync",
            "text:ReprimandTriggersModule.ViewAssociatedReprimandsAsync",
            "text:StickyModule.DisableStickyMessageAsync", "text:StickyModule.EnableStickyMessageAsync",
            "text:StickyModule.RemoveEntityAsync"
        ]),
        ("Finds the reprimand by the start of its ID among this server's reprimands, then checks its "
        + "category.",
        [
            "interaction:InteractiveReprimandsModule.PardonReprimandAsync",
            "interaction:InteractiveReprimandsModule.PardonReprimandMenuAsync",
            "interaction:InteractiveReprimandsModule.RemoveEntityAsync",
            "interaction:InteractiveReprimandsModule.UpdateReprimandAsync",
            "interaction:InteractiveReprimandsModule.UpdateReprimandMenuAsync",
            "interaction:InteractiveReprimandsModule.ViewReprimandAsync",
            "text:ModifyReprimandsModule.PardonReprimandAsync", "text:ModifyReprimandsModule.RemoveEntityAsync",
            "text:ModifyReprimandsModule.UpdateReprimandAsync", "text:ModifyReprimandsModule.ViewReprimandAsync"
        ]),
        ("Finds the mute, timeout or rule only among this server's records, and the extend button only "
        + "opens the form that does.",
        [
            "interaction:ModerationInteractionHandlers.HandleAutoDeleteAsync",
            "interaction:ModerationInteractionHandlers.HandleAutoToggleAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteDetailsAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteExtendAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteExtendModalAsync",
            "interaction:ModerationInteractionHandlers.HandleMuteUnmuteAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutDetailsAsync",
            "interaction:ModerationInteractionHandlers.HandleTimeoutRemoveAsync"
        ]),
        ("The ID is a user, and the handler acts on that user in this server only.",
        [
            "interaction:InteractiveModerationModule.BanAsync",
            "interaction:InteractiveModerationModule.HardMuteAsync",
            "interaction:InteractiveModerationModule.KickAsync", "interaction:InteractiveModerationModule.MuteAsync",
            "interaction:InteractiveModerationModule.NoteAsync",
            "interaction:InteractiveModerationModule.NoticeAsync",
            "interaction:InteractiveModerationModule.RespondModMenuAsync",
            "interaction:InteractiveModerationModule.TimeoutAsync",
            "interaction:InteractiveModerationModule.WarnAsync",
            "interaction:InteractiveReprimandsModule.ViewUserHistoryAsync",
            "interaction:InteractiveUserModule.ComponentHistoryAsync",
            "interaction:InteractiveUserModule.ComponentInformationAsync",
            "interaction:InteractiveUserModule.ComponentReprimandsAsync"
        ]),
        ("The custom ID names a setting, a category or a trigger of the panel session, which holds this "
        + "server's records.",
        [
            "interaction:InteractiveConfigurationModule.SelectCategoryAsync",
            "interaction:InteractiveConfigurationModule.UpdateStringAsync",
            "interaction:InteractiveConfigurationModule.UpdateTimeSpanAsync",
            "interaction:InteractiveTriggersModule.SelectCategoryAsync",
            "interaction:InteractiveTriggersModule.SelectTriggerAsync"
        ]),
        ("The channel is read only from this server, and the handler checks Manage Messages or the Purge "
        + "scope.",
        [
            "interaction:InteractiveGeneralModule.DeleteMessageAsync"
        ]),
        ("The linked button must belong to this server.",
        [
            "interaction:InteractiveMessagesModule.ViewTemplateAsync"
        ]),
        ("The quoted message is read only from this server's channels that the member can view.",
        [
            "interaction:InteractiveQuoteModule.CollapseQuoteAsync",
            "interaction:InteractiveQuoteModule.ExpandQuoteAsync", "interaction:InteractiveQuoteModule.ShowAllAsync"
        ]),
        ("The custom ID carries the caller's own choices, such as an element or a region, for their own "
        + "listing.",
        [
            "interaction:LibenModule.AddAsync", "interaction:LibenModule.AddInteractiveAsync",
            "interaction:LibenModule.AddModalAsync", "interaction:LibenModule.ListBoxesAsync",
            "interaction:LibenModule.LookingInteractiveAsync"
        ]));

    private const int ModeratorRank = 10;
    private const int TargetRank = 20;
    private const int BotRank = 100;

    private sealed record Handler(string Key, MethodInfo Method, IReadOnlyList<Attribute> Preconditions);

    [Fact]
    public async Task AMemberWithNoScopes_IsRefusedEverywhereNotOpenOnPurpose()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(MemberId);
        var open = new List<string>();
        var handlers = Handlers().ToList();
        var refused = 0;

        foreach (var handler in handlers)
        {
            if (!await PassesAsync(handler, fixture, member))
            {
                refused++;
                continue;
            }

            if (!OpenOnPurpose.ContainsKey(handler.Key)) open.Add(handler.Key);
        }

        output.WriteLine($"{handlers.Count} handlers: {refused} refused, "
            + $"{handlers.Count - refused - open.Count} open on purpose, {open.Count} open by mistake");
        Assert.True(open.Count == 0,
            $"{open.Count} handlers that a member with no scopes passes:{Environment.NewLine}"
            + string.Join(Environment.NewLine, open.Order()));
    }

    [Fact]
    public async Task OpenOnPurpose_NamesOnlyOpenHandlersThatExist()
    {
        var fixture = new AuthorizationFixture();
        var member = fixture.User(MemberId);
        var handlers = Handlers().ToList();
        var stale = new List<string>();

        foreach (var key in OpenOnPurpose.Keys)
        {
            var handler = handlers.FirstOrDefault(h => h.Key == key);
            if (handler is null || !await PassesAsync(handler, fixture, member)) stale.Add(key);
        }

        Assert.True(stale.Count == 0,
            $"Allow-list entries that no longer match an open handler:{Environment.NewLine}"
            + string.Join(Environment.NewLine, stale.Order()));
    }

    // The sweep reaches the guarded handlers too: the member with no scopes is refused, and a Discord administrator, whom
    // the server's first rule allows every scope, passes.
    [Fact]
    public async Task TheSweep_RunsTheGuards()
    {
        var fixture = new AuthorizationFixture();
        var handlers = Handlers().ToList();
        var add = handlers.Single(h => h.Key == "text:PermissionsModule.AddPermissionAsync");
        var admin = Mock.Get(fixture.User(MemberId + 1));
        admin.SetupGet(m => m.GuildPermissions).Returns(new GuildPermissions(administrator: true));

        Assert.False(await PassesAsync(add, fixture, fixture.User(MemberId)));
        Assert.True(await PassesAsync(add, fixture, admin.Object));
        Assert.True(handlers.Count > 400, $"{handlers.Count} handlers");
    }

    // A moderator who ranks below the target names that member or role. The parameter checks must refuse, so the handler
    // never acts on someone the moderator could not act on in Discord. The bot ranks above both, so a refusal is about
    // the moderator.
    [Fact]
    public async Task AModeratorBelowTheTarget_IsRefusedEverywhereNotOpenOnPurpose()
    {
        var fixture = new AuthorizationFixture();
        var moderator = Ranked(fixture.User(MemberId), ModeratorRank);
        var bot = Ranked(fixture.User(MemberId + 1), BotRank);
        Mock.Get(fixture.DiscordGuild)
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(bot);

        var targets = Targets().ToList();
        var open = new List<string>();
        var refused = 0;

        foreach (var (key, parameter) in targets)
        {
            if (!await PassesAsync(parameter, fixture, moderator, TargetOf(fixture, parameter.ParameterType)))
            {
                refused++;
                continue;
            }

            if (!TargetsOpenOnPurpose.ContainsKey(key)) open.Add(key);
        }

        output.WriteLine($"{targets.Count} targets: {refused} refused, "
            + $"{targets.Count - refused - open.Count} open on purpose, {open.Count} open by mistake");
        Assert.True(open.Count == 0,
            $"{open.Count} targets that a moderator below the target passes:{Environment.NewLine}"
            + string.Join(Environment.NewLine, open.Order()));
    }

    [Fact]
    public async Task TargetsOpenOnPurpose_NamesOnlyOpenTargetsThatExist()
    {
        var fixture = new AuthorizationFixture();
        var moderator = Ranked(fixture.User(MemberId), ModeratorRank);
        var bot = Ranked(fixture.User(MemberId + 1), BotRank);
        Mock.Get(fixture.DiscordGuild)
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(bot);

        var targets = Targets().ToList();
        var stale = new List<string>();
        foreach (var key in TargetsOpenOnPurpose.Keys)
        {
            var matches = targets.Where(t => t.Key == key).ToList();
            var open = false;
            foreach (var (_, parameter) in matches)
                open |= await PassesAsync(parameter, fixture, moderator, TargetOf(fixture, parameter.ParameterType));

            if (!open) stale.Add(key);
        }

        Assert.True(stale.Count == 0,
            $"Allow-list entries that no longer match an open target:{Environment.NewLine}"
            + string.Join(Environment.NewLine, stale.Order()));
    }

    // The target check passes a member who ranks below the moderator, so a refusal above is about rank.
    [Fact]
    public async Task TheTargetSweep_LetsAModeratorActBelowTheirRank()
    {
        var fixture = new AuthorizationFixture();
        var moderator = Ranked(fixture.User(MemberId), ModeratorRank);
        var bot = Ranked(fixture.User(MemberId + 1), BotRank);
        Mock.Get(fixture.DiscordGuild)
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(bot);

        var ban = Targets().First(t => t.Key == "text:ModerationModule.BanAsync(user)").Parameter;
        var lower = Ranked(fixture.User(MemberId + 2), ModeratorRank - 5);

        Assert.False(await PassesAsync(ban, fixture, moderator, TargetOf(fixture, ban.ParameterType)));
        Assert.True(await PassesAsync(ban, fixture, moderator, lower));
    }

    // A member who has every scope in one moderation category and none anywhere else. Every category parameter must
    // refuse the other category. A handler the member passes without naming a category must check the category of the
    // record it acts on, unless the list below names it with the reason it is safe.
    [Fact]
    public async Task AMemberWithTheScopeInAnotherCategory_IsRefusedEverywhereNotOpenOnPurpose()
    {
        var fixture = new AuthorizationFixture();
        var stranger = fixture.User(MemberId + 1);
        var (member, raids, spam) = CategoryMember(fixture);

        var categoryChecks = 0;
        var open = new List<string>();
        var passed = 0;
        foreach (var handler in Handlers())
        {
            foreach (var (name, check) in CategoryChecks(handler))
            {
                categoryChecks++;
                if (await PassesAsync(check, fixture, member, spam)) open.Add($"{handler.Key}({name}) in Spam");
                if (!await PassesAsync(check, fixture, member, raids)) open.Add($"{handler.Key}({name}) refuses Raids");
            }

            if (OpenOnPurpose.ContainsKey(handler.Key) || await PassesAsync(handler, fixture, stranger)) continue;
            if (!await PassesAsync(handler, fixture, member)) continue;

            passed++;
            if (!OpenToAnotherCategory.ContainsKey(handler.Key)) open.Add(handler.Key);
        }

        output.WriteLine($"{categoryChecks} category parameters refuse Spam and pass Raids; {passed} handlers pass "
            + $"without naming a category, {open.Count} open by mistake");
        Assert.True(open.Count == 0,
            $"{open.Count} handlers or category parameters open to another category:{Environment.NewLine}"
            + string.Join(Environment.NewLine, open.Distinct().Order()));
    }

    [Fact]
    public async Task OpenToAnotherCategory_NamesOnlyOpenHandlersThatExist()
    {
        var fixture = new AuthorizationFixture();
        var (member, _, _) = CategoryMember(fixture);
        var handlers = Handlers().ToList();
        var stale = new List<string>();

        foreach (var key in OpenToAnotherCategory.Keys)
        {
            var matches = handlers.Where(h => h.Key == key).ToList();
            var open = false;
            foreach (var handler in matches)
                open |= await PassesAsync(handler, fixture, member);

            if (!open) stale.Add(key);
        }

        Assert.True(stale.Count == 0,
            $"Allow-list entries that no longer match a handler open to another category:{Environment.NewLine}"
            + string.Join(Environment.NewLine, stale.Order()));
    }

    // A member of another server can type an ID from this server, or send a button or form whose custom ID carries one.
    // Every handler that takes an ID that way must look it up only among this server's records, and the list below
    // names how each one does.
    [Fact]
    public void EveryHandlerThatTakesAnId_ChecksItAgainstThisServer()
    {
        var unreviewed = IdHandlers()
            .Where(key => !IdsCheckedAgainstThisServer.ContainsKey(key))
            .Distinct()
            .Order()
            .ToList();

        output.WriteLine($"{IdHandlers().Distinct().Count()} handlers take an ID, {unreviewed.Count} unreviewed");
        Assert.True(unreviewed.Count == 0,
            $"{unreviewed.Count} handlers take an ID with no reviewed server check:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unreviewed));
    }

    [Fact]
    public void IdsCheckedAgainstThisServer_NamesOnlyHandlersThatTakeAnId()
    {
        var handlers = IdHandlers().ToHashSet();
        var stale = IdsCheckedAgainstThisServer.Keys.Where(k => !handlers.Contains(k)).Order().ToList();

        Assert.True(stale.Count == 0,
            $"Allow-list entries that no longer take an ID:{Environment.NewLine}" + string.Join(Environment.NewLine, stale));
    }

    // Wildcards in a custom ID become the handler's first parameters, and parameters named as IDs are typed in.
    private static IEnumerable<string> IdHandlers()
        => Handlers().Where(h => CustomIdOf(h.Method)?.Contains('*') == true
                || h.Method.GetParameters().Any(p => IsIdName(p.Name) && IsIdType(p.ParameterType)))
            .Select(h => h.Key);

    private static string? CustomIdOf(MethodInfo method)
        => method.GetCustomAttribute<ComponentInteractionAttribute>()?.CustomId
            ?? method.GetCustomAttribute<ModalInteractionAttribute>()?.CustomId;

    private static bool IsIdName(string? name)
        => name is not null && (name.Equals("id", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Id", StringComparison.Ordinal) || name.EndsWith("IdString", StringComparison.Ordinal));

    private static bool IsIdType(Type type)
        => type == typeof(string) || type == typeof(Guid) || type == typeof(ulong) || type == typeof(ulong?)
            || type == typeof(Guid?);

    private static (IGuildUser Member, ModerationCategory Raids, ModerationCategory Spam) CategoryMember(
        AuthorizationFixture fixture)
    {
        var member = fixture.User(MemberId);
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.All, member);
        return (member, raids, spam);
    }

    private static IEnumerable<(string Name, Attribute Check)> CategoryChecks(Handler handler)
        => handler.Method.GetParameters().SelectMany(p => p.GetCustomAttributes(true).OfType<Attribute>()
            .Where(a => a.GetType().Name == "CheckCategoryAttribute")
            .Select(a => (p.Name ?? "?", a)));

    private static IGuildUser Ranked(IGuildUser user, int rank)
    {
        Mock.Get(user).SetupGet(u => u.Hierarchy).Returns(rank);
        return user;
    }

    /// <summary>Every handler parameter that names a member, a user or a role, alone or in a list.</summary>
    private static IEnumerable<(string Key, System.Reflection.ParameterInfo Parameter)> Targets()
        => Handlers().SelectMany(h => h.Method.GetParameters()
            .Where(p => IsTarget(p.ParameterType))
            .Select(p => ($"{h.Key}({p.Name})", p)));

    private static bool IsTarget(Type type)
    {
        var element = ElementOf(type) ?? type;
        return typeof(IUser).IsAssignableFrom(element) || typeof(IRole).IsAssignableFrom(element);
    }

    private static Type? ElementOf(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        if (type == typeof(string)) return null;

        return type.GetInterfaces().Append(type)
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(i => i.GetGenericArguments()[0])
            .FirstOrDefault();
    }

    /// <summary>
    ///     A member or a role that ranks above the moderator, in the shape the parameter takes. A list holds the one
    ///     target, typed as the declared element when that is an interface the mock implements.
    /// </summary>
    private static object TargetOf(AuthorizationFixture fixture, Type type)
    {
        var element = ElementOf(type);
        var role = typeof(IRole).IsAssignableFrom(element ?? type);
        object target = role
            ? Mock.Of<IRole>(r => r.Id == 78 && r.Position == TargetRank && r.Guild == fixture.DiscordGuild
                && r.Name == "Admin")
            : Ranked(fixture.User(77), TargetRank);

        if (element is null) return target;

        var shape = element.IsInstanceOfType(target) ? element : role ? typeof(IRole) : typeof(IGuildUser);
        var list = Array.CreateInstance(shape, 1);
        list.SetValue(target, 0);
        return list;
    }

    private static async Task<bool> PassesAsync(
        System.Reflection.ParameterInfo parameter, AuthorizationFixture fixture, IGuildUser member, object value)
    {
        foreach (var check in parameter.GetCustomAttributes(true).OfType<Attribute>()
            .Where(a => a is Discord.Commands.ParameterPreconditionAttribute
                or Discord.Interactions.ParameterPreconditionAttribute))
        {
            if (!await PassesAsync(check, fixture, member, value)) return false;
        }

        return true;
    }

    // Discord.Net runs ungrouped preconditions together, and a named group passes when any one member passes.
    private static async Task<bool> PassesAsync(Handler handler, AuthorizationFixture fixture, IGuildUser member)
    {
        var checks = handler.Preconditions.Where(p => AuthorizationKinds.Contains(p.GetType().Name)).ToList();
        foreach (var group in checks.GroupBy(GroupOf))
        {
            var results = new List<bool>();
            foreach (var check in group)
                results.Add(await PassesAsync(check, fixture, member));

            var passed = group.Key is null ? results.All(r => r) : results.Any(r => r);
            if (!passed) return false;
        }

        return true;
    }

    private static string? GroupOf(Attribute precondition) => precondition switch
    {
        CommandsPrecondition c     => c.Group,
        InteractionsPrecondition i => i.Group,
        _                          => null
    };

    private static async Task<bool> PassesAsync(
        Attribute precondition, AuthorizationFixture fixture, IGuildUser member, object? value = null)
    {
        var client = new Mock<IDiscordClient>();
        client.SetupGet(c => c.TokenType).Returns(TokenType.Bot);
        client.Setup(c => c.GetApplicationInfoAsync(It.IsAny<RequestOptions>()))
            .ReturnsAsync(Mock.Of<IApplication>(a => a.Owner == Mock.Of<IUser>(u => u.Id == OwnerOfTheBot)));
        var channel = Mock.Of<ITextChannel>(c => c.Id == 902 && c.Guild == fixture.DiscordGuild);

        switch (precondition)
        {
            case CommandsPrecondition command:
            {
                var context = new Mock<Discord.Commands.ICommandContext>();
                context.SetupGet(c => c.Client).Returns(client.Object);
                context.SetupGet(c => c.Guild).Returns(fixture.DiscordGuild);
                context.SetupGet(c => c.Channel).Returns(channel);
                context.SetupGet(c => c.User).Returns(member);
                context.SetupGet(c => c.Message).Returns(Mock.Of<IUserMessage>());
                return (await command.CheckPermissionsAsync(context.Object, null!, fixture.Services)).IsSuccess;
            }
            case InteractionsPrecondition interaction:
            {
                var context = new Mock<IInteractionContext>();
                context.SetupGet(c => c.Client).Returns(client.Object);
                context.SetupGet(c => c.Guild).Returns(fixture.DiscordGuild);
                context.SetupGet(c => c.Channel).Returns(channel);
                context.SetupGet(c => c.User).Returns(member);
                context.SetupGet(c => c.Interaction).Returns(Mock.Of<IDiscordInteraction>());
                return (await interaction.CheckRequirementsAsync(context.Object, null!, fixture.Services)).IsSuccess;
            }
            case Discord.Commands.ParameterPreconditionAttribute parameter:
            {
                var context = new Mock<Discord.Commands.ICommandContext>();
                context.SetupGet(c => c.Client).Returns(client.Object);
                context.SetupGet(c => c.Guild).Returns(fixture.DiscordGuild);
                context.SetupGet(c => c.Channel).Returns(channel);
                context.SetupGet(c => c.User).Returns(member);
                return (await parameter.CheckPermissionsAsync(
                    context.Object, await CommandParameterAsync(), value, fixture.Services)).IsSuccess;
            }
            case Discord.Interactions.ParameterPreconditionAttribute parameter:
            {
                var context = new Mock<IInteractionContext>();
                context.SetupGet(c => c.Client).Returns(client.Object);
                context.SetupGet(c => c.Guild).Returns(fixture.DiscordGuild);
                context.SetupGet(c => c.Channel).Returns(channel);
                context.SetupGet(c => c.User).Returns(member);
                context.SetupGet(c => c.Interaction).Returns(Mock.Of<IDiscordInteraction>());
                var info = Mock.Of<Discord.Interactions.IParameterInfo>(p => p.Name == "target" && p.Command.Name == "sweep");
                return (await parameter.CheckRequirementsAsync(context.Object, info, value, fixture.Services)).IsSuccess;
            }
            default:
                return true;
        }
    }

    // Parameter checks name the command in their refusal, and only CommandService builds a command parameter.
    private static async Task<Discord.Commands.ParameterInfo> CommandParameterAsync()
    {
        var module = await new Discord.Commands.CommandService().CreateModuleAsync("sweep", m => m
            .AddCommand("sweep", (_, _, _, _) => Task.CompletedTask, c => c.AddParameter<string>("target", _ => { })));
        return module.Commands[0].Parameters[0];
    }

    /// <summary>
    ///     Every text command and interaction handler, the way Discord.Net builds modules: public types, their nested
    ///     modules, and every instance method with a command attribute. A handler's preconditions include those of its
    ///     module, the module's base classes and the modules around it.
    /// </summary>
    private static IEnumerable<Handler> Handlers()
    {
        var types = typeof(BotEntry).Assembly.DefinedTypes
            .Where(t => (t.IsPublic || t.IsNestedPublic) && !t.IsAbstract)
            .Where(t => t.DeclaringType is null || !IsModule(t.DeclaringType));

        return types.Where(IsModule).SelectMany(t => Handlers(t, []));
    }

    private static bool IsModule(Type type)
        => typeof(IModuleBase).IsAssignableFrom(type) || typeof(IInteractionModuleBase).IsAssignableFrom(type);

    private static IEnumerable<Handler> Handlers(Type module, IReadOnlyList<Attribute> parents)
    {
        var text = typeof(IModuleBase).IsAssignableFrom(module);
        var preconditions = parents.Concat(Preconditions(module)).ToList();

        var methods = module.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => !m.IsStatic && !m.IsGenericMethod)
            .Where(m => text
                ? m.IsDefined(typeof(CommandAttribute))
                : m.IsDefined(typeof(SlashCommandAttribute)) || m.IsDefined(typeof(ComponentInteractionAttribute))
                || m.IsDefined(typeof(ModalInteractionAttribute)) || m.IsDefined(typeof(ContextCommandAttribute)));

        foreach (var method in methods)
        {
            var key = $"{(text ? "text" : "interaction")}:{module.Name}.{method.Name}";
            var parameters = method.GetParameters()
                .SelectMany(p => p.GetCustomAttributes(true).OfType<Attribute>())
                .Where(a => a is Discord.Commands.ParameterPreconditionAttribute
                    or Discord.Interactions.ParameterPreconditionAttribute);
            yield return new Handler(key, method, preconditions.Concat(Preconditions(method)).Concat(parameters).ToList());
        }

        foreach (var nested in module.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Where(IsModule))
        {
            foreach (var handler in Handlers(nested, preconditions))
                yield return handler;
        }
    }

    private static IEnumerable<Attribute> Preconditions(MemberInfo member)
        => member.GetCustomAttributes(true).OfType<Attribute>()
            .Where(a => a is CommandsPrecondition or InteractionsPrecondition);
}
