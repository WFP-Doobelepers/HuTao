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

        public bool Verbose { get; set; } = true;
    }
}