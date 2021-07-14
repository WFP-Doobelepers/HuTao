using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation
{
    public class AutoModerationRules
    {
        public Guid Id { get; set; }

        public Guid? BanTriggerId { get; set; }

        public Guid? KickTriggerId { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual AntiSpamRules? AntiSpamRules { get; set; }

        public virtual BanTrigger? BanTrigger { get; set; }

        public virtual ICollection<MuteTrigger> MuteTriggers { get; set; }
            = new List<MuteTrigger>();

        public virtual ICollection<NoticeTrigger> NoticeTriggers { get; set; }
            = new List<NoticeTrigger>();

        public virtual ICollection<Censor> Censors { get; init; } = new List<Censor>();

        public virtual KickTrigger? KickTrigger { get; set; }
    }

    public class AutoModerationRulesConfiguration : IEntityTypeConfiguration<AutoModerationRules>
    {
        public void Configure(EntityTypeBuilder<AutoModerationRules> builder)
        {
            builder.HasOne(r => r.BanTrigger).WithOne()
                .HasForeignKey<AutoModerationRules>(t => t.BanTriggerId);

            builder.HasOne(r => r.KickTrigger).WithOne()
                .HasForeignKey<AutoModerationRules>(t => t.KickTriggerId);

            builder.HasMany(r => r.MuteTriggers).WithOne()
                .HasForeignKey(t => t.AutoModerationRulesId);

            builder.HasMany(r => r.NoticeTriggers).WithOne()
                .HasForeignKey(t => t.AutoModerationRulesId);
        }
    }
}