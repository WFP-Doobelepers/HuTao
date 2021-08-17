using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation
{
    public class ModerationRules
    {
        public Guid Id { get; set; }

        public virtual AntiSpamRules? AntiSpamRules { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual ICollection<Trigger> Triggers { get; set; }
            = new List<Trigger>();

        public TimeSpan? CensorTimeRange { get; set; }

        public TimeSpan? NoticeAutoPardonLength { get; set; }

        public TimeSpan? WarningAutoPardonLength { get; set; }

        public ulong GuildId { get; set; }

        public ulong? MuteRoleId { get; set; }
    }
}