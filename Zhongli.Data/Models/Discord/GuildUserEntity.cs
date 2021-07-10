using System;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        public int WarningCount { get; set; }

        public string Username { get; set; }

        public string? Nickname { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong GuildId { get; set; }

        public ushort DiscriminatorValue { get; set; }
    }

    public class GuildUserEntityConfiguration : IEntityTypeConfiguration<GuildUserEntity>
    {
        public void Configure(EntityTypeBuilder<GuildUserEntity> builder)
        {
            builder.HasKey(w => new { w.Id, w.GuildId });
        }
    }
}