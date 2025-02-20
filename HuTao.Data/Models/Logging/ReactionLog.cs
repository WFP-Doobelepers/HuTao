using System;
using System.Diagnostics.CodeAnalysis;
using Discord.WebSocket;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Discord.Reaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Logging;

public interface IReactionEntity : IMessageEntity
{
    ReactionEntity Emote { get; set; }
}

public class ReactionLog : ILog, IReactionEntity
{
    protected ReactionLog() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ReactionLog(GuildUserEntity user, SocketReaction reaction, ReactionEntity emote)
    {
        LogDate   = DateTimeOffset.UtcNow;
        User      = user;
        ChannelId = reaction.Channel.Id;
        MessageId = reaction.MessageId;
        Emote     = emote;
    }

    public Guid Id { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual GuildUserEntity User { get; set; } = null!;

    public DateTimeOffset LogDate { get; set; }

    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }

    public ulong MessageId { get; set; }

    public virtual ReactionEntity Emote { get; set; } = null!;

    public ulong UserId { get; set; }
}

public class ReactionLogConfiguration : IEntityTypeConfiguration<ReactionLog>
{
    public void Configure(EntityTypeBuilder<ReactionLog> builder) => builder.AddUserNavigation(r => r.User);
}