using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class BanTrigger : WarningTrigger, IBan
    {
        public BanTrigger(uint amount, bool retroactive, uint deleteDays, TimeSpan? length)
            : base(amount, retroactive)
        {
            DeleteDays = deleteDays;
            Length     = length;
        }

        public uint DeleteDays { get; set; }

        public TimeSpan? Length { get; set; }
    }

    public class BanTriggerConfiguration : IEntityTypeConfiguration<BanTrigger>
    {
        public void Configure(EntityTypeBuilder<BanTrigger> builder)
        {
            builder
                .Property(t => t.Length)
                .HasColumnName(nameof(BanTrigger.Length));
        }
    }
}