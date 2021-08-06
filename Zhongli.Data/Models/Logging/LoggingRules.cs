using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class LoggingRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong? ModerationChannelId { get; set; }

        public string? ReprimandAppealMessage { get; set; }

        public ReprimandNoticeType NotifyReprimands { get; set; }

        public ReprimandNoticeType ShowAppealOnReprimands { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public LoggingOptions Options { get; set; }
    }
}