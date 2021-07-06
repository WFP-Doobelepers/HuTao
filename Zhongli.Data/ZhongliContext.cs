using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data
{
    public class ZhongliContext : DbContext
    {
        public ZhongliContext(DbContextOptions<ZhongliContext> options) : base(options) { }

        public DbSet<GuildEntity> Guilds { get; init; }

        public DbSet<GuildUserEntity> Users { get; init; }

        public DbSet<ChannelAuthorization> ChannelAuthorizations { get; init; }

        public DbSet<GuildAuthorization> GuildAuthorizations { get; init; }

        public DbSet<PermissionAuthorization> PermissionAuthorizations { get; init; }

        public DbSet<RoleAuthorization> RoleAuthorizations { get; init; }

        public DbSet<UserAuthorization> UserAuthorizations { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZhongliContext).Assembly);
        }
    }
}