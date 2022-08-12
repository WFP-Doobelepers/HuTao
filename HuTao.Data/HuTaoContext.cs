using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Components;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.TimeTracking;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Data;

public class HuTaoContext : DbContext
{
    public HuTaoContext(DbContextOptions<HuTaoContext> options) : base(options) { }

    public DbSet<AttachmentConfiguration> AttachmentConfigurations { get; init; } = null!;

    public DbSet<Ban> BanHistory { get; init; } = null!;

    public DbSet<BanAction> BanActions { get; init; } = null!;

    public DbSet<Button> Buttons { get; init; } = null!;

    public DbSet<Censor> Censors { get; init; } = null!;

    public DbSet<Censored> CensoredHistory { get; init; } = null!;

    public DbSet<ChannelCriterion> ChannelCriteria { get; init; } = null!;

    public DbSet<CriterionExclusion> CriteriaExclusions { get; set; } = null!;

    public DbSet<DuplicateConfiguration> DuplicateConfigurations { get; init; } = null!;

    public DbSet<EmojiConfiguration> EmojiConfigurations { get; init; } = null!;

    public DbSet<EmojiEntity> Emojis { get; init; } = null!;

    public DbSet<EmojiExclusion> EmojiExclusions { get; set; } = null!;

    public DbSet<EmoteEntity> Emotes { get; init; } = null!;

    public DbSet<EnumChannel> EnumChannels { get; init; } = null!;

    public DbSet<ExpirableReprimand> ExpirableReprimands { get; init; } = null!;

    public DbSet<Filtered> FilteredHistory { get; init; } = null!;

    public DbSet<GuildEntity> Guilds { get; init; } = null!;

    public DbSet<GuildUserEntity> Users { get; init; } = null!;

    public DbSet<HardMute> HardMuteHistory { get; init; } = null!;

    public DbSet<InviteConfiguration> InviteConfigurations { get; init; } = null!;

    public DbSet<InviteExclusion> InviteExclusions { get; set; } = null!;

    public DbSet<Kick> KickHistory { get; init; } = null!;

    public DbSet<KickAction> KickActions { get; init; } = null!;

    public DbSet<LinkConfiguration> LinkConfigurations { get; init; } = null!;

    public DbSet<LinkExclusion> LinkExclusions { get; set; } = null!;

    public DbSet<MentionConfiguration> MentionConfigurations { get; init; } = null!;

    public DbSet<MessageConfiguration> MessageConfigurations { get; init; } = null!;

    public DbSet<MessageDeleteLog> MessageDeleteLogs { get; init; } = null!;

    public DbSet<MessagesDeleteLog> MessagesDeleteLogs { get; init; } = null!;

    public DbSet<Mute> MuteHistory { get; init; } = null!;

    public DbSet<MuteAction> MuteActions { get; init; } = null!;

    public DbSet<NewLineConfiguration> NewLineConfigurations { get; init; } = null!;

    public DbSet<Note> NoteHistory { get; init; } = null!;

    public DbSet<NoteAction> NoteActions { get; init; } = null!;

    public DbSet<Notice> NoticeHistory { get; init; } = null!;

    public DbSet<NoticeAction> NoticeActions { get; init; } = null!;

    public DbSet<PermissionCriterion> PermissionCriteria { get; init; } = null!;

    public DbSet<ReactionDeleteLog> ReactionDeleteLogs { get; init; } = null!;

    public DbSet<ReplyConfiguration> ReplyConfigurations { get; init; } = null!;

    public DbSet<ReprimandTrigger> ReprimandTriggers { get; init; } = null!;

    public DbSet<RoleAction> RoleActions { get; init; } = null!;

    public DbSet<RoleCriterion> RoleCriteria { get; init; } = null!;

    public DbSet<RoleEntity> Roles { get; init; } = null!;

    public DbSet<RoleMentionExclusion> RoleExclusions { get; set; } = null!;

    public DbSet<RoleReprimand> RoleHistory { get; init; } = null!;

    public DbSet<SelectMenu> SelectMenus { get; init; } = null!;

    public DbSet<TimeTracking> TimeTrackingRules { get; init; } = null!;

    public DbSet<UserCriterion> UserCriteria { get; init; } = null!;

    public DbSet<UserMentionExclusion> UserExclusions { get; set; } = null!;

    public DbSet<Warning> WarningHistory { get; init; } = null!;

    public DbSet<WarningAction> WarningActions { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(HuTaoContext).Assembly);
}