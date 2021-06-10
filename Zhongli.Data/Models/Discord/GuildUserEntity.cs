using System;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;

namespace Zhongli.Data.Models.Discord
{
    public class GuildUserEntity
    {
        protected GuildUserEntity() { }

        public GuildUserEntity(IGuildUser user)
        {
            Id = user.Id;

            CreatedAt = user.CreatedAt;
            JoinedAt  = user.JoinedAt;

            Username = user.Username;
            Nickname = user.Nickname;

            DiscriminatorValue = user.DiscriminatorValue;

            GuildId = user.GuildId;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? JoinedAt { get; set; }

        public string Username { get; set; }

        public string? Nickname { get; set; }

        public ushort DiscriminatorValue { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }
    }
}