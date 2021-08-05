using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class BanCensor : Censor, IBan
    {
        protected BanCensor() { }

        public BanCensor(uint deleteDays, TimeSpan? length, string pattern,
            RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            DeleteDays = deleteDays;
            Length     = length;
        }

        public uint DeleteDays { get; set; }

        public TimeSpan? Length { get; set; }
    }

    public class BanCensorConfiguration : IEntityTypeConfiguration<BanCensor>
    {
        public void Configure(EntityTypeBuilder<BanCensor> builder)
        {
            builder
                .Property(c => c.Length)
                .HasColumnName(nameof(BanCensor.Length));
        }
    }
}