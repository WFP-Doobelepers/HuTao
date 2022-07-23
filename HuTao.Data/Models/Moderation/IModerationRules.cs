using System;
using System.Collections.Generic;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Logging;

namespace HuTao.Data.Models.Moderation;

public interface IModerationRules
{
    public bool CensorNicknames { get; set; }

    public bool CensorUsernames { get; set; }

    public bool ReplaceMutes { get; set; }

    public ICollection<Criterion> CensorExclusions { get; set; }

    public ModerationLoggingRules? Logging { get; set; }

    public string? NameReplacement { get; set; }

    public TimeSpan? AutoReprimandCooldown { get; set; }

    public TimeSpan? CensoredExpiryLength { get; set; }

    public TimeSpan? FilteredExpiryLength { get; set; }

    public TimeSpan? NoticeExpiryLength { get; set; }

    public TimeSpan? WarningExpiryLength { get; set; }

    public ulong? HardMuteRoleId { get; set; }

    public ulong? MuteRoleId { get; set; }
}