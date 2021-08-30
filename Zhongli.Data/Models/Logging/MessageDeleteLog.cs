using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Logging
{
    public class MessageDeleteLog : DeleteLog, IMessageEntity
    {
        protected MessageDeleteLog() { }

        public MessageDeleteLog(IMessageEntity message, ActionDetails? details) : base(details)
        {
            ChannelId = message.ChannelId;
            GuildId   = message.GuildId;
            MessageId = message.MessageId;
            UserId    = message.UserId;
        }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public ulong MessageId { get; set; }

        public ulong UserId { get; set; }
    }

    public class MessageDeleteLogConfiguration : IEntityTypeConfiguration<MessageDeleteLog>
    {
        public void Configure(EntityTypeBuilder<MessageDeleteLog> builder)
        {
            builder
                .Property(l => l.ChannelId)
                .HasColumnName(nameof(IMessageEntity.ChannelId));

            builder
                .Property(l => l.GuildId)
                .HasColumnName(nameof(IMessageEntity.GuildId));

            builder
                .Property(l => l.MessageId)
                .HasColumnName(nameof(IMessageEntity.MessageId));

            builder
                .Property(l => l.UserId)
                .HasColumnName(nameof(IMessageEntity.UserId));
        }
    }
}