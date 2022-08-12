using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Moderation.Infractions;

namespace HuTao.Data.Models.Logging;

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

    [Column(nameof(IReactionEntity.ChannelId))]
    public ulong ChannelId { get; set; }

    [Column(nameof(IReactionEntity.GuildId))]
    public ulong GuildId { get; set; }

    [Column(nameof(IReactionEntity.MessageId))]
    public ulong MessageId { get; set; }

    public virtual ReactionEntity Emote { get; set; } = null!;

    [Column(nameof(IReactionEntity.UserId))]
    public ulong UserId { get; set; }
}