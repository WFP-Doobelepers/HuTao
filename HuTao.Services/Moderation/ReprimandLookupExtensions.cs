using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fergun.Interactive;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core;
using HuTao.Services.Interactive.Paginator;

namespace HuTao.Services.Moderation;

public static class ReprimandLookupExtensions
{
    /// <summary>
    ///     Finds a reprimand by its ID or the start of it. When several match, the selection lists only the reprimands
    ///     the user has the scope for, so it never shows the others. When none of the matches are allowed, the first
    ///     one is returned without being shown, so the caller's own category check refuses it with a clear message.
    /// </summary>
    public static async Task<Reprimand?> TryFindReprimandAsync(
        this InteractiveService interactive, AuthorizationService auth, Context context,
        IEnumerable<Reprimand> reprimands, AuthorizationScope scope, string id, bool ephemeral = false)
    {
        if (id.Length < 2)
            return null;

        var matches = reprimands
            .Where(r => r.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allowed = await auth.WhereReprimandsAuthorizedAsync(context, scope, matches);
        if (allowed.Count == 0)
            return matches.FirstOrDefault();

        return await interactive.TryFindEntityV2Async(
            context, allowed, r => r.ToEmbedBuilder(true), r => r.Id.ToString(), id,
            ephemeral, TimeSpan.FromMinutes(2));
    }
}
