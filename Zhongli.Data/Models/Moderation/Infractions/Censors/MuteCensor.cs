using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class MuteCensor : Censor, IMute
    {
        protected MuteCensor() { }

        public MuteCensor(TimeSpan? length, string pattern, RegexOptions options = RegexOptions.None) : base(pattern,
            options)
        {
            Length = length;
        }

        public TimeSpan? Length { get; set; }
    }

    public class MuteCensorConfiguration : IEntityTypeConfiguration<MuteCensor>
    {
        public void Configure(EntityTypeBuilder<MuteCensor> builder)
        {
            builder
                .Property(c => c.Length)
                .HasColumnName(nameof(MuteCensor.Length));
        }
    }
}