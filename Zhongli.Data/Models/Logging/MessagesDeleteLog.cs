using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Logging
{
    public class MessagesDeleteLog : DeleteLog, IChannelEntity, IGuildEntity
    {
        protected MessagesDeleteLog() { }

        public MessagesDeleteLog(IEnumerable<IMessageEntity> messages, IGuildChannel channel,
            ActionDetails? details) : base(details)
        {
            ChannelId = channel.Id;
            GuildId   = channel.Guild.Id;

            Messages = messages.Select(m => new MessageDeleteLog(m, details)).ToList();
        }

        public virtual ICollection<MessageDeleteLog> Messages { get; set; }

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
}