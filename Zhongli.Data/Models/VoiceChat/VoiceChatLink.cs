using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.VoiceChat;

public class VoiceChatLink : IGuildUserEntity
{
    public Guid Id { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual GuildUserEntity Owner { get; set; } = null!;

    public ulong TextChannelId { get; set; }

    public ulong VoiceChannelId { get; set; }

    public ulong GuildId { get; set; }

    public ulong UserId { get; set; }
}

public class VoiceChatLinkConfiguration : IEntityTypeConfiguration<VoiceChatLink>
{
    public void Configure(EntityTypeBuilder<VoiceChatLink> builder) => builder.AddUserNavigation(v => v.Owner);
}