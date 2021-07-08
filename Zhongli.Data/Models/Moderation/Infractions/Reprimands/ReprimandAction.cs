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
            UserId  = details.UserId;
            GuildId = details.GuildId;

            Type   = details.Type;
            Reason = details.Reason;
        }

        public Guid Id { get; set; }

        public ModerationActionType Type { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public string? Reason { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public virtual ModerationAction Action { get; set; }
    }

    public class ReprimandActionConfiguration : IEntityTypeConfiguration<ReprimandAction>
    {
        public void Configure(EntityTypeBuilder<ReprimandAction> builder)
        {
            builder.HasOne(r => r.User)
                .WithMany().HasForeignKey(r => new { r.UserId, r.GuildId });
        }
    }

    public enum ModerationActionType
    {
        Added,
        Removed
    }
}