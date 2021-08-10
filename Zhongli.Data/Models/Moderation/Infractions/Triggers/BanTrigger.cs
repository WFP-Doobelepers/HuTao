using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class BanTrigger : Trigger, IBan
    {
        public BanTrigger(uint amount, TriggerSource source, TriggerMode mode, uint deleteDays, TimeSpan? length)
            : base(amount, source, mode)
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