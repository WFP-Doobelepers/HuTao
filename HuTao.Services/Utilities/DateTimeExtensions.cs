using System;
using Discord;
using static Discord.TimestampTag;
using static Discord.TimestampTagStyles;

namespace HuTao.Services.Utilities;

public static class DateTimeExtensions
{
    public static string ToUniversalTimestamp(this DateTimeOffset date)
        => $"{Format.Bold($"{FromDateTimeOffset(date, Relative)}")} ({FromDateTimeOffset(date)})";

    public static string ToUniversalTimestamp(this TimeSpan length)
        => ToUniversalTimestamp(DateTimeOffset.UtcNow + length);

    public static TimeSpan Clamp(this TimeSpan timeSpan, TimeSpan minimum, TimeSpan maximum) => timeSpan switch
    {
        _ when timeSpan < minimum => minimum,
        _ when timeSpan > maximum => maximum,
        _                         => timeSpan
    };

    public static TimeSpan TimeLeft(this DateTimeOffset date) => date.ToUniversalTime() - DateTimeOffset.UtcNow;

    public static TimeSpan TimeLeft(this DateTime date) => date.ToUniversalTime() - DateTime.UtcNow;

    public static TimestampTag ToDiscordTimestamp(this TimeSpan length, TimestampTagStyles style = ShortDateTime)
        => FromDateTimeOffset(DateTimeOffset.UtcNow + length, style);
}