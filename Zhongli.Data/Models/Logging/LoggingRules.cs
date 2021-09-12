using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation;

namespace Zhongli.Data.Models.Logging
{
    public class LoggingRules
    {
        public Guid Id { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual ICollection<EnumChannel<LogType>> LoggingChannels { get; set; }
            = new List<EnumChannel<LogType>>();

        public ReprimandNoticeType NotifyReprimands { get; set; } = ReprimandNoticeType.None;

        public ReprimandNoticeType ShowAppealOnReprimands { get; set; } = ReprimandNoticeType.All;

        public string? ReprimandAppealMessage { get; set; }

        public ulong GuildId { get; set; }
    }
}