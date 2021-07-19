using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

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

            GuildId = user.Guild.Id;
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

        public int HistoryCount<T>() where T : ReprimandAction => Reprimands<T>().Count();

        private IEnumerable<T> Reprimands<T>() => Guild.ReprimandHistory
            .Where(r => r.User.Id == Id)
            .OfType<T>();

        public int ReprimandCount<T>() where T : ICountable
            => (int) Reprimands<T>().Sum(w => w.Amount);
    }

    public class GuildUserEntityConfiguration : IEntityTypeConfiguration<GuildUserEntity>
    {
        public void Configure(EntityTypeBuilder<GuildUserEntity> builder)
        {
            builder.HasKey(w => new { w.Id, w.GuildId });
        }
    }
}