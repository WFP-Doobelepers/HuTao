using System;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Discord
{
    public class GuildUserEntity
    {
        protected GuildUserEntity() { }

        public GuildUserEntity(IGuildUser user) : this(user, user.Guild)
        {
            JoinedAt = user.JoinedAt;
            Nickname = user.Nickname;
        }

        public GuildUserEntity(SocketUser user) : this(user, user.MutualGuilds.ElementAt(0)) { }

        public GuildUserEntity(IUser user, IGuild guild)
        {
            Id      = user.Id;
            GuildId = guild.Id;

            CreatedAt          = user.CreatedAt;
            Username           = user.Username;
            DiscriminatorValue = user.DiscriminatorValue;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public string Username { get; set; }

        public string? Nickname { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong GuildId { get; set; }

        public ushort DiscriminatorValue { get; set; }

        public override string ToString() => $"{Username}#{DiscriminatorValue}";
    }

    public class GuildUserEntityConfiguration : IEntityTypeConfiguration<GuildUserEntity>
    {
        public void Configure(EntityTypeBuilder<GuildUserEntity> builder)
        {
            builder.HasKey(w => new { w.Id, w.GuildId });
        }
    }
}