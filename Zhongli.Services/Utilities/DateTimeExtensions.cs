using System;
using System.Collections.Generic;
using Discord;
using static Zhongli.Services.Utilities.DateTimeExtensions.TimestampStyle;

namespace Zhongli.Services.Utilities
{
    public static class DateTimeExtensions
    {
        public enum TimestampStyle
        {
            ShortTime,
            LongTime,
            ShortDate,
            LongDate,
            ShortDateTime,
            LongDateTime,
            RelativeTime,
            Default = ShortDateTime
        }

        private static readonly Dictionary<TimestampStyle, string> TimestampStyles = new()
        {
            [ShortTime]     = "t",
            [LongTime]      = "T",
            [ShortDate]     = "d",
            [LongDate]      = "D",
            [ShortDateTime] = "f",
            [LongDateTime]  = "F",
            [RelativeTime]  = "R"
        };

        public static string ToDiscordTimestamp(this TimeSpan length, TimestampStyle style = Default)
            => ToDiscordTimestamp(DateTimeOffset.Now + length, style);

        public static string ToDiscordTimestamp(this DateTimeOffset date, TimestampStyle style = Default)
            => $"<t:{date.ToUnixTimeSeconds()}:{TimestampStyles[style]}>";

        public static string ToUniversalTimestamp(this DateTimeOffset date)
            => $"{Format.Bold(date.ToDiscordTimestamp(RelativeTime))} ({date.ToDiscordTimestamp()})";

        public static string ToUniversalTimestamp(this TimeSpan length)
            => ToUniversalTimestamp(DateTimeOffset.Now + length);

        public static TimeSpan TimeLeft(this DateTimeOffset date) => date.ToUniversalTime() - DateTimeOffset.UtcNow;

        public static TimeSpan TimeLeft(this DateTime date) => date.ToUniversalTime() - DateTime.UtcNow;
    }
}