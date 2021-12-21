using System;
using System.Collections.Generic;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging;

public class LoggingRules
{
    public Guid Id { get; set; }

    public virtual GuildEntity Guild { get; set; }

    public virtual ICollection<Criterion> LoggingExclusions { get; set; }
        = new List<Criterion>();

    public virtual ICollection<EnumChannel<LogType>> LoggingChannels { get; set; }
        = new List<EnumChannel<LogType>>();

    public ulong GuildId { get; set; }
}