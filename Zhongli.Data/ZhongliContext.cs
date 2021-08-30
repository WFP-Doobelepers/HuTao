using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
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

        public DbSet<BanAction> BanActions { get; init; }

        public DbSet<Censor> Censors { get; init; }

        public DbSet<Censored> CensoredHistory { get; set; }

        public DbSet<ChannelCriterion> ChannelCriteria { get; init; }

        public DbSet<EmojiEntity> Emojis { get; set; }

        public DbSet<EmoteEntity> Emotes { get; set; }

        public DbSet<GuildEntity> Guilds { get; init; }

        public DbSet<GuildUserEntity> Users { get; init; }

        public DbSet<Kick> KickHistory { get; init; }

        public DbSet<KickAction> KickActions { get; init; }

        public DbSet<MessageDeleteLog> MessageDeleteLogs { get; set; }

        public DbSet<Mute> MuteHistory { get; init; }

        public DbSet<MuteAction> MuteActions { get; init; }

        public DbSet<Note> NoteHistory { get; init; }

        public DbSet<NoteAction> NoteActions { get; init; }

        public DbSet<Notice> NoticeHistory { get; init; }

        public DbSet<NoticeAction> NoticeActions { get; init; }

        public DbSet<PermissionCriterion> PermissionCriteria { get; init; }

        public DbSet<ReactionDeleteLog> ReactionDeleteLogs { get; set; }

        public DbSet<ReprimandTrigger> ReprimandTriggers { get; init; }

        public DbSet<RoleCriterion> RoleCriteria { get; init; }

        public DbSet<TimeTracking> TimeTrackingRules { get; init; }

        public DbSet<UserCriterion> UserCriteria { get; init; }

        public DbSet<Warning> WarningHistory { get; init; }

        public DbSet<WarningAction> WarningActions { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZhongliContext).Assembly);
        }
    }
}