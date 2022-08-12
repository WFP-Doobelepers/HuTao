using System;
using System.Collections.Generic;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Logging;

namespace HuTao.Data.Models.Moderation;

public class ModerationRules : IModerationRules
{
    public Guid Id { get; set; }

    public virtual ICollection<ModerationExclusion> Exclusions { get; set; } = new List<ModerationExclusion>();

    public virtual ICollection<ModerationVariable> Variables { get; set; } = new List<ModerationVariable>();

    public virtual ICollection<Trigger> Triggers { get; set; } = new List<Trigger>();

    public bool CensorNicknames { get; set; }

    public bool CensorUsernames { get; set; }

    public bool ReplaceMutes { get; set; }

    public virtual ICollection<Criterion> CensorExclusions { get; set; } = new List<Criterion>();

    public virtual ModerationLoggingRules? Logging { get; set; }

    public string? NameReplacement { get; set; }

    public TimeSpan? AutoReprimandCooldown { get; set; }

    public TimeSpan? CensoredExpiryLength { get; set; }

    public TimeSpan? FilteredExpiryLength { get; set; }

    public TimeSpan? NoticeExpiryLength { get; set; }

    public TimeSpan? WarningExpiryLength { get; set; }

    public ulong? HardMuteRoleId { get; set; }

    public ulong? MuteRoleId { get; set; }
}