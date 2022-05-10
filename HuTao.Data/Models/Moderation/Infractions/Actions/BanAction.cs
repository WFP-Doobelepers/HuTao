﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class BanAction : ReprimandAction, IBan
{
    public BanAction(uint deleteDays, TimeSpan? length)
    {
        DeleteDays = deleteDays;
        Length     = length;
    }

    public uint DeleteDays { get; set; }

    public TimeSpan? Length { get; set; }
}

public class BanActionConfiguration : IEntityTypeConfiguration<BanAction>
{
    public void Configure(EntityTypeBuilder<BanAction> builder) => builder
        .Property(t => t.Length)
        .HasColumnName(nameof(BanAction.Length));
}