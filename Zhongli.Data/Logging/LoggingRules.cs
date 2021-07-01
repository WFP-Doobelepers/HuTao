using System;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Logging
{
    public class LoggingRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ModerationChannelId { get; set; }

        public virtual GuildEntity Guild { get; set; }
    }
}
