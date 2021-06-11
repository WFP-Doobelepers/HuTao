using System;
using System.Collections.Generic;

namespace Zhongli.Data.Models.Moderation
{
    public class AutoModerationRules
    {
        public Guid Id { get; set; }

        public virtual AntiSpamRules AntiSpamRules { get; set; }

        public virtual ICollection<WarningTrigger> WarningTriggers { get; set; }
    }
}