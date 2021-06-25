using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.VoiceChat
{
    public class VoiceChatLink
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(GuildId))] public virtual GuildEntity Guild { get; set; }

        [ForeignKey(nameof(OwnerId))] public virtual GuildUserEntity Owner { get; set; }

        public ulong GuildId { get; set; }

        public ulong OwnerId { get; set; }

        public ulong TextChannelId { get; set; }

        public ulong VoiceChannelId { get; set; }
    }
    
    public class VoiceChatLinkConfiguration : IEntityTypeConfiguration<VoiceChatLink>
    {
        public void Configure(EntityTypeBuilder<VoiceChatLink> builder)
        {
            builder.HasOne(v => v.Owner)
                .WithMany().HasForeignKey(v => new { v.OwnerId, v.GuildId });
        }
    }
}