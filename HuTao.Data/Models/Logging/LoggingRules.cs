using System;
using System.Collections.Generic;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Logging;

public class LoggingRules
{
    public Guid Id { get; set; }

    public bool UploadAttachments { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual ICollection<Criterion> LoggingExclusions { get; set; }
        = new List<Criterion>();

    public virtual ICollection<EnumChannel<LogType>> LoggingChannels { get; set; }
        = new List<EnumChannel<LogType>>();

    public ulong GuildId { get; set; }
}