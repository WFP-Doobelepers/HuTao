using System;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Discord.Reaction;

namespace Zhongli.Data.Models.Logging
{
    public class ReactionLog : ILog, IMessageEntity
    {
        protected ReactionLog() { }

        public ReactionLog(GuildUserEntity user, SocketReaction reaction, LogType logType, ReactionEntity emote)
        {
            LogType = logType;
            LogDate = DateTimeOffset.Now;

            User      = user;
            MessageId = reaction.MessageId;
            ChannelId = reaction.Channel.Id;

            Emote = emote;
        }

        public Guid Id { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public virtual ReactionEntity Emote { get; set; }

        public bool IsCategory { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public DateTimeOffset LogDate { get; set; }

        public LogType LogType { get; set; }

        public ulong MessageId { get; set; }

        public ulong UserId { get; set; }
    }

    public class ReactionLogConfiguration : IEntityTypeConfiguration<ReactionLog>
    {
        public void Configure(EntityTypeBuilder<ReactionLog> builder) { builder.AddUserNavigation(r => r.User); }
    }
}