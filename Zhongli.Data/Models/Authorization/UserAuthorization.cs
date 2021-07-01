using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class UserAuthorization : IAuthorizationRule
    {
        public Guid Id { get; set; }

        public ulong UserId { get; set; }

        public ulong AddedById { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public DateTimeOffset Date { get; set; }

        public virtual GuildUserEntity AddedBy { get; set; }

        public AuthorizationScope Scope { get; set; }
    }

    public class UserAuthorizationConfiguration : IEntityTypeConfiguration<UserAuthorization>
    {
        public void Configure(EntityTypeBuilder<UserAuthorization> builder)
        {
            builder
                .HasOne(a => a.User)
                .WithMany().HasForeignKey(a => new { a.UserId, a.GuildId });

            builder
                .HasOne(a => a.AddedBy)
                .WithMany().HasForeignKey(a => new { a.AddedById, a.GuildId });
        }
    }
}