using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message.Components;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Data.Models.TimeTracking;

namespace Zhongli.Data;

public class ZhongliContext : DbContext
{
    public ZhongliContext(DbContextOptions<ZhongliContext> options) : base(options) { }

    public DbSet<Ban> BanHistory { get; init; } = null!;

    public DbSet<BanAction> BanActions { get; init; } = null!;

    public DbSet<Button> Buttons { get; set; } = null!;

    public DbSet<Censor> Censors { get; init; } = null!;

    public DbSet<Censored> CensoredHistory { get; set; } = null!;

    public DbSet<ChannelCriterion> ChannelCriteria { get; init; } = null!;

    public DbSet<EmojiEntity> Emojis { get; set; } = null!;

    public DbSet<EmoteEntity> Emotes { get; set; } = null!;

    public DbSet<EnumChannel> EnumChannels { get; set; } = null!;

    public DbSet<GuildEntity> Guilds { get; init; } = null!;

    public DbSet<GuildUserEntity> Users { get; init; } = null!;

    public DbSet<Kick> KickHistory { get; init; } = null!;

    public DbSet<KickAction> KickActions { get; init; } = null!;

    public DbSet<MessageDeleteLog> MessageDeleteLogs { get; set; } = null!;

    public DbSet<Mute> MuteHistory { get; init; } = null!;

    public DbSet<MuteAction> MuteActions { get; init; } = null!;

    public DbSet<Note> NoteHistory { get; init; } = null!;

    public DbSet<NoteAction> NoteActions { get; init; } = null!;

    public DbSet<Notice> NoticeHistory { get; init; } = null!;

    public DbSet<NoticeAction> NoticeActions { get; init; } = null!;

    public DbSet<PermissionCriterion> PermissionCriteria { get; init; } = null!;

    public DbSet<ReactionDeleteLog> ReactionDeleteLogs { get; set; } = null!;

    public DbSet<ReprimandTrigger> ReprimandTriggers { get; init; } = null!;

    public DbSet<RoleCriterion> RoleCriteria { get; init; } = null!;

    public DbSet<SelectMenu> SelectMenus { get; set; } = null!;

    public DbSet<TimeTracking> TimeTrackingRules { get; init; } = null!;

    public DbSet<UserCriterion> UserCriteria { get; init; } = null!;

    public DbSet<Warning> WarningHistory { get; init; } = null!;

    public DbSet<WarningAction> WarningActions { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZhongliContext).Assembly);
}