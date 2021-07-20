using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class LoggingRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong? ModerationChannelId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public LoggingOptions Options { get; set; }
    }

    [Flags]
    public enum LoggingOptions
    {
        Default    = 0,
        Verbose    = 1 << 0,
        Silent     = 1 << 1,
        NotifyUser = 1 << 2,
        Anonymous  = 1 << 3
    }
}