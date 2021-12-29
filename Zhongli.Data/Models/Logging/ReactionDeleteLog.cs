using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Logging;

public class ReactionDeleteLog : DeleteLog, IReactionEntity
{
    protected ReactionDeleteLog() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ReactionDeleteLog(ReactionEntity emote, SocketReaction reaction, ActionDetails? details) : base(details)
    {
        Emote = emote;

        var channel = (IGuildChannel) reaction.Channel;
        ChannelId = channel.Id;
        GuildId   = channel.Guild.Id;

        MessageId = reaction.MessageId;
        UserId    = reaction.UserId;
    }

    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }

    public ulong MessageId { get; set; }

    public virtual ReactionEntity Emote { get; set; } = null!;

    public ulong UserId { get; set; }
}

public class ReactionDeleteLogConfiguration : IEntityTypeConfiguration<ReactionDeleteLog>
{
    public void Configure(EntityTypeBuilder<ReactionDeleteLog> builder)
    {
        builder
            .Property(l => l.ChannelId)
            .HasColumnName(nameof(IReactionEntity.ChannelId));

        builder
            .Property(l => l.GuildId)
            .HasColumnName(nameof(IReactionEntity.GuildId));

        builder
            .Property(l => l.MessageId)
            .HasColumnName(nameof(IReactionEntity.MessageId));

        builder
            .Property(l => l.UserId)
            .HasColumnName(nameof(IReactionEntity.UserId));
    }
}