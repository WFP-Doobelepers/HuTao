using System.Threading;
using System.Threading.Tasks;
using Discord.Interactions;
using Humanizer;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Moderation;

public static class RunningReprimandAccess
{
    /// <summary>
    ///     Why the member may not act on the mute or timeout from a list button, or null when they may. The list can be
    ///     older than the record, so the record must still be running. The member needs the scope in the record's
    ///     category, which is the check the command that made the record ran.
    /// </summary>
    public static async Task<string?> RefuseActAsync(
        this AuthorizationService auth, Context context, AuthorizationScope scope, ExpirableReprimand reprimand,
        CancellationToken cancellationToken = default)
    {
        if (!reprimand.IsRunning())
            return $"This {reprimand.GetType().Name.Humanize(LetterCasing.LowerCase)} has already ended.";

        if (await auth.IsReprimandAuthorizedAsync(context, scope, reprimand, cancellationToken))
            return null;

        return reprimand.Category is { } category
            ? $"You do not have the {scope} scope in the {category.Name} category."
            : $"You do not have the {scope} scope.";
    }

    public static Task<string?> RefuseActAsync(
        this AuthorizationService auth, SocketInteractionContext context, AuthorizationScope scope,
        ExpirableReprimand reprimand)
        => auth.RefuseActAsync(new InteractionContext(context), scope, reprimand);

    /// <summary>Whether the mute or timeout is still running, by the rules that the lists use to show it.</summary>
    public static bool IsRunning(this ExpirableReprimand reprimand)
        => reprimand.Status is not (ReprimandStatus.Expired or ReprimandStatus.Pardoned or ReprimandStatus.Deleted)
            && reprimand.IsActive();
}
