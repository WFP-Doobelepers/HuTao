using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Kick : ReprimandAction
    {
        protected Kick() { }

        public Kick(ReprimandDetails details) : base(details) { }
    }

    public class KickConfiguration : IEntityTypeConfiguration<Kick>
    {
        public void Configure(EntityTypeBuilder<Kick> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });

            builder.HasOne(r => r.Moderator)
                .WithMany().HasForeignKey(r => new { r.ModeratorId, r.GuildId });
        }
    }
}