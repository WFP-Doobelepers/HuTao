using System;

namespace Zhongli.Services.Utilities
{
    public static class DateTimeExtensions
    {
        public static TimeSpan TimeLeft(this DateTimeOffset date) => date.ToUniversalTime() - DateTimeOffset.UtcNow;

        public static TimeSpan TimeLeft(this DateTime date) => date.ToUniversalTime() - DateTime.UtcNow;
    }
}