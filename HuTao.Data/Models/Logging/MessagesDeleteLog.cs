using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Moderation.Infractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }
}

public class MessagesBulkDeleteLogConfiguration : IEntityTypeConfiguration<MessagesDeleteLog>
{
    public void Configure(EntityTypeBuilder<MessagesDeleteLog> builder)
    {
        builder
            .Property(l => l.ChannelId)
            .HasColumnName(nameof(IChannelEntity.ChannelId));

        builder
            .Property(l => l.GuildId)
            .HasColumnName(nameof(IGuildEntity.GuildId));
    }
}