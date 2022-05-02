using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Logging;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class ModerationCategory : IModerationRules
{
    protected ModerationCategory() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationCategory(string name, ICriteriaOptions? options, IGuildUser? moderator)
    {
        Name          = name;
        Authorization = options?.ToAuthorizationGroups(moderator).ToList() ?? new List<AuthorizationGroup>();
    }

    public Guid Id { get; set; }

    public virtual ICollection<AuthorizationGroup> Authorization { get; set; } = new List<AuthorizationGroup>();

    public static ModerationCategory All { get; } = new("All", null, null);

    public virtual ModerationLoggingRules? LoggingRules { get; set; }

    public string Name { get; set; } = null!;

    public bool ReplaceMutes { get; set; }

    public virtual ICollection<Criterion> CensorExclusions { get; set; } = new List<Criterion>();

    public TimeSpan? CensorTimeRange { get; set; }

    public TimeSpan? NoticeExpiryLength { get; set; }

    public TimeSpan? WarningExpiryLength { get; set; }

    public ulong? MuteRoleId { get; set; }
}