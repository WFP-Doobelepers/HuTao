using System.Collections.Generic;
using System.Linq;

namespace HuTao.Data.Config;

/// <summary>
///     How many days the bot keeps what members write and do. PRIVACY.md keeps personal information for at most a
///     month, so each default is 30 days. The bot's config section changes them, for example with
///     <c>Retention:MessageLogDays</c>.
/// </summary>
public class RetentionConfig
{
    /// <summary>Saved messages and their edits, and the logs of deleted messages and of reactions.</summary>
    public int MessageLogDays { get; init; } = 30;

    /// <summary>The transcript of a closed ticket, and the description its member wrote when opening it.</summary>
    public int TicketTranscriptDays { get; init; } = 30;

    /// <summary>Everything the bot holds for a server, counted from the moment the server removed the bot.</summary>
    public int RemovedServerDays { get; init; } = 30;

    /// <summary>Background jobs that failed. A failed job keeps the ids it was given and its error.</summary>
    public int FailedJobDays { get; init; } = 30;

    /// <summary>The settings below one day, which would delete what the bot saved as soon as it saved it.</summary>
    public IEnumerable<string> TooShort()
        => new[]
            {
                (nameof(MessageLogDays), MessageLogDays), (nameof(TicketTranscriptDays), TicketTranscriptDays),
                (nameof(RemovedServerDays), RemovedServerDays), (nameof(FailedJobDays), FailedJobDays)
            }
            .Where(s => s.Item2 < 1)
            .Select(s => $"Retention:{s.Item1}");
}
