using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Logging;

namespace HuTao.Data.Models.Moderation;

public interface ICategory
{
    public ModerationCategory? Category { get; }
}

public class ModerationCategory : IModerationRules
{
    protected ModerationCategory() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationCategory(string name, ICriteriaOptions? options, IGuildUser? moderator)
    {
        Name = name;
        Authorization = options?.ToAuthorizationGroups(AuthorizationScope.All, moderator).ToList()
            ?? new List<AuthorizationGroup>();
    }

    public Guid Id { get; set; }

    public virtual ICollection<AuthorizationGroup> Authorization { get; set; } = new List<AuthorizationGroup>();

    public static ModerationCategory Default { get; } = new("Default", null, null);

    public string Name { get; set; } = null!;

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