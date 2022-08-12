using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Moderation.Infractions;

namespace HuTao.Data.Models.Logging;

public class MessagesDeleteLog : DeleteLog, IChannelEntity, IGuildEntity
{
    protected MessagesDeleteLog() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public MessagesDeleteLog(IEnumerable<IMessageEntity> messages, IGuildChannel channel,
        ActionDetails? details) : base(details)
    {
        ChannelId = channel.Id;
        GuildId   = channel.Guild.Id;

        Messages = messages.Select(m => new MessageDeleteLog(m, details)).ToList();
    }

    public virtual ICollection<MessageDeleteLog> Messages { get; set; } = null!;

    [Column(nameof(IChannelEntity.ChannelId))]
    public ulong ChannelId { get; set; }

    [Column(nameof(IGuildEntity.GuildId))]
    public ulong GuildId { get; set; }
}