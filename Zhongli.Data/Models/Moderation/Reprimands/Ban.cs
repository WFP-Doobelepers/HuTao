using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Ban : ReprimandActionBase
    {
        protected Ban() { }

        public Ban(ReprimandDetails details, uint deleteDays) : base(details) { DeleteDays = deleteDays; }

        public uint DeleteDays { get; set; }
    }

    public class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        public void Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });

            builder.HasOne(r => r.Moderator)
                .WithMany().HasForeignKey(r => new { r.ModeratorId, r.GuildId });
        }
    }
}