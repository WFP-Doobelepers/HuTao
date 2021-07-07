using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IModerationAction
    {
        ModerationAction Action { get; set; }
    }

    public class ModerationAction
    {
        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public ulong GuildId { get; set; }

        public ulong ModeratorId { get; set; }
    }

    public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
    {
        public void Configure(EntityTypeBuilder<ModerationAction> builder)
        {
            builder.HasOne(r => r.Moderator)
                .WithMany().HasForeignKey(r => new { r.ModeratorId, r.GuildId });
        }
    }
}