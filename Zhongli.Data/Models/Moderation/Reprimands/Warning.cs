using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Warning : ReprimandAction
    {
        protected Warning() { }

        public Warning(ReprimandDetails details, uint amount) : base(details) { Amount = amount; }

        public uint Amount { get; init; }
    }

    public class WarningConfiguration : IEntityTypeConfiguration<Warning>
    {
        public void Configure(EntityTypeBuilder<Warning> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });

            builder.HasOne(r => r.Moderator)
                .WithMany().HasForeignKey(r => new { r.ModeratorId, r.GuildId });
        }
    }
}