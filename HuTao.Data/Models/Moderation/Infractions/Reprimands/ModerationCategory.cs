using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class ModerationCategory
{
    protected ModerationCategory() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ModerationCategory(string name, ICriteriaOptions? options, IGuildUser? moderator)
    {
        Name          = name;
        Authorization = options?.ToAuthorizationGroups(moderator).ToList() ?? new List<AuthorizationGroup>();
    }

    public Guid Id { get; set; }

    public virtual ICollection<AuthorizationGroup> Authorization { get; set; }
        = new List<AuthorizationGroup>();

    public static ModerationCategory All { get; } = new("All", null, null);

    public string Name { get; set; } = null!;
}