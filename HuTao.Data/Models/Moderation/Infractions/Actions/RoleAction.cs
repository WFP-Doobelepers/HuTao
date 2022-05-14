using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HuTao.Data.Models.Discord.Message.Linking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

    public TimeSpan? Length { get; set; }

    public virtual ICollection<RoleTemplate> Roles { get; set; } = null!;
}

public class RoleActionConfiguration : IEntityTypeConfiguration<RoleAction>
{
    public void Configure(EntityTypeBuilder<RoleAction> builder) => builder
        .Property(r => r.Length)
        .HasColumnName(nameof(RoleAction.Length));
}