using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class MuteCensor : Censor, IMute
    {
        protected MuteCensor() { }

        public MuteCensor(string pattern, ICensorOptions? options, TimeSpan? length) : base(pattern, options)
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