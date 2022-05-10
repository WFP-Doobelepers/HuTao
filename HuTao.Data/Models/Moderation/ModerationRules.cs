using System;
using System.Collections.Generic;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation;

public interface IModerationRules
{
    public bool ReplaceMutes { get; set; }

    public ICollection<Criterion> CensorExclusions { get; set; }

    public TimeSpan? CensorTimeRange { get; set; }

    public TimeSpan? NoticeExpiryLength { get; set; }

    public TimeSpan? WarningExpiryLength { get; set; }

    public ulong? MuteRoleId { get; set; }
}

public class ModerationRules : IModerationRules
{
    public Guid Id { get; set; }

    public virtual AntiSpamRules? AntiSpamRules { get; set; }

    public virtual ICollection<Trigger> Triggers { get; set; } = new List<Trigger>();

    public bool ReplaceMutes { get; set; }

    public virtual ICollection<Criterion> CensorExclusions { get; set; } = new List<Criterion>();

    public TimeSpan? CensorTimeRange { get; set; }

    public TimeSpan? NoticeExpiryLength { get; set; }

    public TimeSpan? WarningExpiryLength { get; set; }

    public ulong? MuteRoleId { get; set; }
}