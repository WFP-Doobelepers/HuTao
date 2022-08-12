using System.ComponentModel.DataAnnotations.Schema;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Moderation.Infractions;

namespace HuTao.Data.Models.Logging;

public class MessageDeleteLog : DeleteLog, IMessageEntity
{
    protected MessageDeleteLog() { }

    public MessageDeleteLog(IGuild guild, IMessage message, ActionDetails? details) : base(details)
    {
        GuildId   = guild.Id;
        MessageId = message.Id;
        ChannelId = message.Channel.Id;
        UserId    = message.Author.Id;
    }

    public MessageDeleteLog(IMessageEntity message, ActionDetails? details) : base(details)
    {
        ChannelId = message.ChannelId;
        GuildId   = message.GuildId;
        MessageId = message.MessageId;
        UserId    = message.UserId;
    }

    [Column(nameof(IMessageEntity.ChannelId))]
    public ulong ChannelId { get; set; }

    [Column(nameof(IMessageEntity.GuildId))]
    public ulong GuildId { get; set; }

    [Column(nameof(IMessageEntity.MessageId))]
    public ulong MessageId { get; set; }

    [Column(nameof(IMessageEntity.UserId))]
    public ulong UserId { get; set; }

    public override string ToString() => $"[{MessageId}]({this.JumpUrl()})";
}