using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Data.Models.TimeTracking;
using HuTao.Data.Models.VoiceChat;

namespace HuTao.Data.Models.Discord;

public class GuildEntity
{
    protected GuildEntity() { }

    public GuildEntity(ulong id) { Id = id; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public virtual GenshinTimeTrackingRules? GenshinRules { get; set; }

    public virtual ICollection<AuthorizationGroup> AuthorizationGroups { get; set; }
        = new List<AuthorizationGroup>();

    public virtual ICollection<DeleteLog> DeleteLogs { get; set; }
        = new List<DeleteLog>();

    public virtual ICollection<LinkedButton> LinkedButtons { get; set; }
        = new List<LinkedButton>();

    public virtual ICollection<LinkedCommand> LinkedCommands { get; set; }
        = new List<LinkedCommand>();

    public virtual ICollection<MessageLog> MessageLogs { get; set; }
        = new List<MessageLog>();

    public virtual ICollection<ModerationCategory> ModerationCategories { get; set; }
        = new List<ModerationCategory>();

    public virtual ICollection<ModerationTemplate> ModerationTemplates { get; set; }
        = new List<ModerationTemplate>();

    public virtual ICollection<ReactionLog> ReactionLogs { get; set; }
        = new List<ReactionLog>();

    public virtual ICollection<Reprimand> ReprimandHistory { get; set; }
        = new List<Reprimand>();

    public virtual ICollection<StickyMessage> StickyMessages { get; set; }
        = new List<StickyMessage>();

    public virtual ICollection<TemporaryRole> TemporaryRoles { get; set; }
        = new List<TemporaryRole>();

    public virtual ICollection<TemporaryRoleMember> TemporaryRoleMembers { get; set; }
        = new List<TemporaryRoleMember>();

    public virtual LoggingRules LoggingRules { get; set; } = null!;

    public virtual ModerationLoggingRules ModerationLoggingRules { get; set; } = null!;

    public virtual ModerationRules ModerationRules { get; set; } = null!;

    public virtual VoiceChatRules? VoiceChatRules { get; set; }
}