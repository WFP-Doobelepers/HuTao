using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using HuTao.Data.Models.Discord;
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
}
