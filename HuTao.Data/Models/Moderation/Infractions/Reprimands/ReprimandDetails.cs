using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public record ReprimandDetails(
        IUser User, IGuildUser Moderator,
        string? Reason, Trigger? Trigger = null,
        Context? Context = null, ModerationCategory? Category = null,
        ReprimandResult? Result = null, bool Ephemeral = false, bool Modify = false)
    : ActionDetails(Moderator.Id, Moderator.Guild.Id, Reason)
{
    public ReprimandDetails(
        Context context, IUser user,
        string? reason, IEnumerable<ModerationVariable>? variables,
        Trigger? trigger = null, ModerationCategory? category = null,
        ReprimandResult? result = null, bool ephemeral = false, bool modify = false)
        : this(user, (IGuildUser) context.User, GetReason(reason, variables), trigger, context,
            category == ModerationCategory.Default ? null : category ?? trigger?.Category,
            result, ephemeral, modify) { }

    public IGuild Guild => Moderator.Guild;

    public async Task<IGuildUser?> GetUserAsync() => User as IGuildUser ?? await Moderator.Guild.GetUserAsync(User.Id);

    private static string? GetReason(string? reason, IEnumerable<ModerationVariable>? variables)
    {
        if (string.IsNullOrWhiteSpace(reason) || variables is null) return reason;
        var result = variables.Aggregate(reason, (result, variable)
            => Regex.Replace(result,
                $@"(?<!\\)[$](({variable.Name})\b|" +
                $@"[{{]\s*({variable.Name})(\s*:(?<args>.+?)\s*)?\s*[}}])",
                variable.Value, RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1)));
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}