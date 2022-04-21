using System;
using Discord;
using Humanizer;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Services.Moderation;

public static class TriggerExtensions
{
    public static bool IsTriggered(this ITrigger trigger, uint amount) => trigger.Mode switch
    {
        TriggerMode.Exact       => amount == trigger.Amount,
        TriggerMode.Retroactive => amount >= trigger.Amount,
        TriggerMode.Multiple    => amount % trigger.Amount is 0 && amount is not 0,
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger.Mode,
            "Invalid trigger mode.")
    };

    public static string GetTitle(this ReprimandAction action) => action switch
    {
        BanAction     => nameof(Ban),
        KickAction    => nameof(Kick),
        MuteAction    => nameof(Mute),
        NoteAction    => nameof(Note),
        NoticeAction  => nameof(Notice),
        WarningAction => nameof(Warning),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action,
            "Invalid ReprimandAction.")
    };

    public static string GetTitle(this Trigger trigger)
    {
        var title = trigger switch
        {
            Censor           => nameof(Censor),
            ReprimandTrigger => nameof(ReprimandTrigger),
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger,
                "Invalid trigger type.")
        };

        title = title
            .Replace(nameof(Trigger), string.Empty)
            .Humanize(LetterCasing.Title);

        return $"{title}: {trigger.Id}";
    }

    public static string GetTriggerDetails(this Trigger trigger)
    {
        var action = trigger switch
        {
            Censor c           => $"{Format.Code(c.Pattern)} ({c.Options.Humanize()})",
            ReprimandTrigger r => $"{r.Source.Humanize().Pluralize()}",
            _ => throw new ArgumentOutOfRangeException(
                nameof(trigger), trigger, "Invalid trigger type.")
        };

        return $"{trigger.GetTriggerMode()} {action}";
    }

    public static string GetTriggerMode(this ITrigger trigger)
        => $"{trigger.Mode} {Format.Code($"{trigger.Amount}")}";
}