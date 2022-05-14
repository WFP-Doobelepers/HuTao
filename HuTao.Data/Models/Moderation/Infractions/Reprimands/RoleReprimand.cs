using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Discord.Message.Linking;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class RoleReprimand : ExpirableReprimand, IRoleReprimand
{
    protected RoleReprimand() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public RoleReprimand(TimeSpan? length, ICollection<RoleTemplate> roles, ReprimandDetails details) : base(length,
        details)
    {
        Length = length;
        Roles  = roles;
    }

    public virtual ICollection<RoleTemplate> Roles { get; set; } = null!;
}