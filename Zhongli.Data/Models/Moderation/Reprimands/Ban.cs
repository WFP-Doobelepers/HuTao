using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Ban : IModerationAction
    {
        public Guid Id { get; set; }

        public uint DeleteDays { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public string? Reason { get; set; }
    }

    public class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        public void Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasOne(w => w.Moderator);
            builder.HasOne(w => w.User);
        }
    }
}