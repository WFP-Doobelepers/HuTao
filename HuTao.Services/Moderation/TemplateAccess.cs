using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Moderation;

public static class TemplateAccess
{
    /// <summary>
    ///     Whether the member may apply the template. The reprimand goes in the template's category, or in the member's
    ///     default category when the template has none, so the member needs the template's scope in that category.
    /// </summary>
    public static Task<bool> MayApplyAsync(
        this AuthorizationService auth, Context context, ModerationTemplate template,
        CancellationToken cancellationToken = default)
        => auth.IsCategoryAuthorizedAsync(context, template.Scope, template.Category, cancellationToken);

    public static Task<bool> MayApplyAsync(
        this AuthorizationService auth, SocketCommandContext context, ModerationTemplate template)
        => auth.MayApplyAsync(new CommandContext(context), template);

    public static Task<bool> MayApplyAsync(
        this AuthorizationService auth, SocketInteractionContext context, ModerationTemplate template)
        => auth.MayApplyAsync(new InteractionContext(context), template);

    /// <summary>
    ///     Why the member may not save a template for the action, or null when they may. Anyone with the template's scope
    ///     can apply it with the bot's authority, so the member may only save an action they may take themselves, in the
    ///     server or in the template's category. The guild owner and Discord administrators may save any.
    /// </summary>
    public static async Task<string?> RefuseSaveAsync(
        this AuthorizationService auth, Context context, IGuildUser member, ReprimandAction action,
        ModerationCategory? category = null, CancellationToken cancellationToken = default)
    {
        // The role commands accept Discord's Manage Roles permission in place of the scope.
        if (action is RoleAction && member.GuildPermissions.ManageRoles)
            return null;

        return await auth.FindUnheldScopeAsync(context, member, ScopeOf(action), category, cancellationToken) is { } unheld
            ? $"You can only save a template for an action you may take yourself, and you do not have the {unheld} scope."
            : null;
    }

    public static Task<string?> RefuseSaveAsync(
        this AuthorizationService auth, SocketCommandContext context, ReprimandAction action,
        ModerationCategory? category = null)
        => auth.RefuseSaveAsync(new CommandContext(context), (IGuildUser) context.User, action, category);

    public static Task<string?> RefuseSaveAsync(
        this AuthorizationService auth, SocketInteractionContext context, ReprimandAction action,
        ModerationCategory? category = null)
        => auth.RefuseSaveAsync(new InteractionContext(context), (IGuildUser) context.User, action, category);

    /// <summary>The scope that the moderation command for the action checks.</summary>
    public static AuthorizationScope ScopeOf(ReprimandAction action) => action switch
    {
        BanAction                     => AuthorizationScope.Ban,
        KickAction                    => AuthorizationScope.Kick,
        MuteAction                    => AuthorizationScope.Mute,
        HardMuteAction                => AuthorizationScope.HardMute,
        TimeoutAction                 => AuthorizationScope.Timeout,
        WarningAction or NoticeAction => AuthorizationScope.Warning,
        NoteAction                    => AuthorizationScope.Note,
        RoleAction                    => AuthorizationScope.ManageRoles,
        _                             => AuthorizationScope.All
    };
}
