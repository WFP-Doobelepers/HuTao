using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public class LoggingRules
    {
        public Guid Id { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public LoggingOptions Options { get; set; } = LoggingOptions.None;

        public ReprimandNoticeType NotifyReprimands { get; set; } = ReprimandNoticeType.None;

        public ReprimandNoticeType ShowAppealOnReprimands { get; set; } = ReprimandNoticeType.All;

        public string? ReprimandAppealMessage { get; set; }

        public ulong GuildId { get; set; }

        public ulong? MessageLogChannelId { get; set; }

        public ulong? ModerationChannelId { get; set; }

        public ulong? ReactionLogChannelId { get; set; }
    }
}