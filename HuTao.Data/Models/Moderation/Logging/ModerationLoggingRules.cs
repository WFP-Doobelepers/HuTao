using System;

namespace HuTao.Data.Models.Moderation.Logging;

public class ModerationLoggingRules
{
    public Guid Id { get; set; }

    public bool IgnoreDuplicates { get; set; }

    public LogReprimandType? HistoryReprimands { get; set; }

    public LogReprimandType? SilentReprimands { get; set; }

    public LogReprimandType? SummaryReprimands { get; set; }

    public virtual ModerationLogChannelConfig? ModeratorLog { get; set; }

    public virtual ModerationLogChannelConfig? PublicLog { get; set; }

    public virtual ModerationLogConfig? CommandLog { get; set; }

    public virtual ModerationLogConfig? UserLog { get; set; }
}