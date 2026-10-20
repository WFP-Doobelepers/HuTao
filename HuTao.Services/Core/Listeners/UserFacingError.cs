using System;
using Microsoft.Extensions.Logging;

namespace HuTao.Services.Core.Listeners;

/// <summary>
///     What a command failure shows to the person who ran it. A failure from a bad argument or a missing permission
///     says why. An exception says only a short ID, because its text can name files, queries or other internals; the
///     full exception goes to the log under the same ID.
/// </summary>
public static class UserFacingError
{
    /// <summary>A short ID that links the message a user saw to the log entry.</summary>
    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    ///     A failed precondition or argument is the user's doing and a normal outcome, so it only reaches the debug log.
    ///     An exception is a bug and is an error.
    /// </summary>
    public static LogLevel LevelFor(bool isException) => isException ? LogLevel.Error : LogLevel.Debug;

    /// <summary>The message for an unexpected failure.</summary>
    public static string ForException(string id)
        => $"Something went wrong running that command. Tell the bot's owner the error ID `{id}`.";

    /// <summary>
    ///     The message to show for <paramref name="reason" />, and the ID to log the full exception under, which is
    ///     null when the reason is safe to show.
    /// </summary>
    public static (string Message, string? Id) Describe(bool isException, string error, string? reason)
    {
        if (!isException) return ($"{error}: {reason}", null);

        var id = NewId();
        return (ForException(id), id);
    }
}
