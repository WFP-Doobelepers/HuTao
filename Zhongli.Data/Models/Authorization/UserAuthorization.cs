using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class UserAuthorization : AuthorizationRule
    {
        protected UserAuthorization() { }

        public UserAuthorization(AuthorizationScope scope, IGuildUser moderator, ulong userId)
            : base(scope, moderator)
        {
            UserId = userId;
        }

        public ulong UserId { get; set; }

        public virtual GuildUserEntity User { get; set; }
    }

    public class UserAuthorizationConfiguration : IEntityTypeConfiguration<UserAuthorization>
    {
        public void Configure(EntityTypeBuilder<UserAuthorization> builder)
        {
            builder
                .HasOne(a => a.User)
                .WithMany().HasForeignKey(a => new { a.UserId, a.GuildId });
        }
    }
}