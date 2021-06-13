using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation
{
    public class ReprimandAction : IModerationAction
    {
        public Guid Id { get; set; }

        public Reprimand Reprimand { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public string? Reason { get; set; }

        public static ReprimandAction FromWarning(Warning warning) =>
            new()
            {
                Reprimand = Reprimand.Warning,
                Date      = warning.Date,
                Guild     = warning.Guild,
                Moderator = warning.Moderator,
                User      = warning.User,
                Reason    = warning.Reason
            };
    }

    public class ReprimandActionConfiguration : IEntityTypeConfiguration<ReprimandAction>
    {
        public void Configure(EntityTypeBuilder<ReprimandAction> builder)
        {
            builder.HasOne(w => w.Moderator);
            builder.HasOne(w => w.User);
        }
    }
}