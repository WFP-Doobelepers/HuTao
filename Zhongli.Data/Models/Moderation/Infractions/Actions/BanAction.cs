using System;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions
{
    public class BanAction : ReprimandAction, IBan
    {
        public BanAction(uint deleteDays, TimeSpan? length)
        {
            DeleteDays = deleteDays;
            Length     = length;
        }

        public override string Action
            => $"Ban {Length?.Humanize() ?? "indefinitely"} and delete {DeleteDays} days of messages";

        public uint DeleteDays { get; set; }

        public TimeSpan? Length { get; set; }
    }

    public class BanActionConfiguration : IEntityTypeConfiguration<BanAction>
    {
        public void Configure(EntityTypeBuilder<BanAction> builder)
        {
            builder
                .Property(t => t.Length)
                .HasColumnName(nameof(BanAction.Length));
        }
    }
}