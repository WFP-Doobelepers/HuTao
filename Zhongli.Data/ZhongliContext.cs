using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Data.Models.TimeTracking;

namespace Zhongli.Data
{
    public class ZhongliContext : DbContext
    {
        public ZhongliContext(DbContextOptions<ZhongliContext> options) : base(options) { }

        public DbSet<Ban> BanHistory { get; init; }

        public DbSet<BanCensor> BanCensors { get; init; }

        public DbSet<BanTrigger> BanTriggers { get; init; }

        public DbSet<ChannelCriterion> ChannelCriteria { get; init; }

        public DbSet<GuildCriterion> GuildCriteria { get; init; }

        public DbSet<GuildEntity> Guilds { get; init; }

        public DbSet<GuildUserEntity> Users { get; init; }

        public DbSet<Kick> KickHistory { get; init; }

        public DbSet<KickCensor> KickCensors { get; init; }

        public DbSet<KickTrigger> KickTriggers { get; init; }

        public DbSet<Mute> MuteHistory { get; init; }

        public DbSet<MuteCensor> MuteCensors { get; init; }

        public DbSet<MuteTrigger> MuteTriggers { get; init; }

        public DbSet<Note> NoteHistory { get; init; }

        public DbSet<NoteCensor> NoteCensors { get; init; }

        public DbSet<Notice> NoticeHistory { get; init; }

        public DbSet<NoticeCensor> NoticeCensors { get; init; }

        public DbSet<NoticeTrigger> NoticeTriggers { get; init; }

        public DbSet<PermissionCriterion> PermissionCriteria { get; init; }

        public DbSet<RoleCriterion> RoleCriteria { get; init; }

        public DbSet<TimeTracking> TimeTrackingRules { get; init; }

        public DbSet<UserCriterion> UserCriteria { get; init; }

        public DbSet<Warning> WarningHistory { get; init; }

        public DbSet<WarningCensor> WarnCensors { get; init; }

        public DbSet<WarningTrigger> WarningTriggers { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZhongliContext).Assembly);
        }
    }
}