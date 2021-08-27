using System;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IModerationAction
    {
        public ModerationAction? Action { get; set; }
    }

    public class ModerationAction : IGuildUserEntity
    {
        protected ModerationAction() { }

        public ModerationAction(ReprimandDetails details) : this(details.Moderator, details.Reason) { }

        public ModerationAction(ModifiedReprimand details) : this(details.Moderator, details.Reason) { }

        public ModerationAction(IGuildUser moderator, string? reason)
        {
            Date = DateTimeOffset.UtcNow;

            GuildId = moderator.Guild.Id;
            UserId  = moderator.Id;

            Reason = reason;
        }

        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public string? Reason { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }
    }

    public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
    {
        public void Configure(EntityTypeBuilder<ModerationAction> builder)
        {
            builder.AddUserNavigation(m => m.Moderator);
        }
    }
}