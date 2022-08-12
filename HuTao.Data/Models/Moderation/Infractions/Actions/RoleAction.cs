using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HuTao.Data.Models.Discord.Message.Linking;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class RoleAction : ReprimandAction, IRoleReprimand
{
    protected RoleAction() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public RoleAction(TimeSpan? length, IRoleTemplateOptions options)
    {
        Length = length;
        Roles  = options.RoleTemplates.ToList();
    }

    [Column(nameof(ILength.Length))]
    public TimeSpan? Length { get; set; }

    public virtual ICollection<RoleTemplate> Roles { get; set; } = null!;
}