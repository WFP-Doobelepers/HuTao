using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation
{
    public class ModerationRules
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual AntiSpamRules? AntiSpamRules { get; set; }

        public virtual ICollection<NoticeTrigger> NoticeTriggers { get; set; }
            = new List<NoticeTrigger>();

        public virtual ICollection<WarningTrigger> WarningTriggers { get; set; }
            = new List<WarningTrigger>();

        public virtual ICollection<Censor> Censors { get; set; }
            = new List<Censor>();

        public ulong? MuteRoleId { get; set; }

        public TimeSpan? NoticeAutoPardonLength { get; set; }

        public TimeSpan? WarningAutoPardonLength { get; set; }

        public string? ReprimandAppealMessage { get; set; }
    }
}