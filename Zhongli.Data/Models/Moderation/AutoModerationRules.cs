using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Triggers;

namespace Zhongli.Data.Models.Moderation
{
    public class AutoModerationRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual AntiSpamRules AntiSpamRules { get; set; }

        public virtual BanTrigger? BanTrigger { get; set; }

        public virtual ICollection<MuteTrigger> MuteTriggers { get; init; } = new List<MuteTrigger>();

        public virtual KickTrigger? KickTrigger { get; set; }
    }
}