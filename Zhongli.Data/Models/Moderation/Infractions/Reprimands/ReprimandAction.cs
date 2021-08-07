using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public abstract class ReprimandAction : IModerationAction
    {
        protected ReprimandAction() { }

        protected ReprimandAction(ReprimandDetails details)
        {
            UserId  = details.User.Id;
            GuildId = details.User.Guild.Id;

            Source = details.Type;

            Action = new ModerationAction(details);
            Status = ReprimandStatus.Added;
        }

        public Guid Id { get; set; }

        public virtual GuildEntity? Guild { get; set; }

        public virtual GuildUserEntity? User { get; set; }

        public virtual ModerationAction? ModifiedAction { get; set; }

        public ModerationSource Source { get; set; }

        public ReprimandStatus Status { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public virtual ModerationAction Action { get; set; }

        public static implicit operator ReprimandResult(ReprimandAction reprimand) => new(reprimand);
    }

    public class ReprimandActionConfiguration : IEntityTypeConfiguration<ReprimandAction>
    {
        public void Configure(EntityTypeBuilder<ReprimandAction> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });
        }
    }
}