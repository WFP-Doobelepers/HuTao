using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

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

        public DbSet<WarningTrigger> WarningTriggers { get; init; }

        public DbSet<Ban> BanHistory { get; init; }

        public DbSet<Kick> KickHistory { get; init; }

        public DbSet<Mute> MuteHistory { get; init; }

        public DbSet<Warning> WarningHistory { get; init; }

        public DbSet<BanCensor> BanCensors { get; init; }

        public DbSet<KickCensor> KickCensors { get; init; }

        public DbSet<MuteCensor> MuteCensors { get; init; }

        public DbSet<NoteCensor> NoteCensors { get; init; }

        public DbSet<WarnCensor> WarnCensors { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZhongliContext).Assembly);
        }
    }
}