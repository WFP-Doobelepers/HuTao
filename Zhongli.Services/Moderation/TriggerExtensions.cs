using System;
using Discord;
using Humanizer;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Services.Moderation
{
    public static class TriggerExtensions
    {
        public static bool IsTriggered(this ITrigger trigger, uint amount)
        {
            return trigger.Mode switch
            {
                TriggerMode.Exact       => amount == trigger.Amount,
                TriggerMode.Retroactive => amount >= trigger.Amount,
                TriggerMode.Multiple    => amount % trigger.Amount is 0 && amount is not 0,
                _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger.Mode,
                    "Invalid trigger mode.")
            };
        }

        public static string GetTitle(this Trigger trigger)
        {
            var title = trigger switch
            {
                Censor           => nameof(Censor),
                ReprimandTrigger => nameof(ReprimandTrigger),
                _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger,
                    "Invalid trigger type.")
            };

            return title.Humanize(LetterCasing.Title);
        }

        public static string GetTriggerDetails(this Trigger trigger)
        {
            return trigger switch
            {
                Censor c           => $"Censor: {Format.Code(c.Pattern)} ({c.Options.Humanize()})",
                ReprimandTrigger r => $"Reprimand: {r.Source.Humanize().Pluralize()}",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(trigger), trigger, "Invalid trigger type.")
            };
        }

        public static string GetTriggerMode(this Trigger trigger)
            => $"{trigger.Mode}: {Format.Code($"{trigger.Amount}")}";
    }
}