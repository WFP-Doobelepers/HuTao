using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Mute : IModerationAction
    {
        public Guid Id { get; set; }

        public DateTimeOffset? End { get; set; }

        public TimeSpan? Length { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public string? Reason { get; set; }

        public ModerationActionType Type { get; set; }
    }

    public class MuteConfiguration : IEntityTypeConfiguration<Mute>
    {
        public void Configure(EntityTypeBuilder<Mute> builder)
        {
            builder.HasOne(w => w.Moderator);
            builder.HasOne(w => w.User);
        }
    }
}