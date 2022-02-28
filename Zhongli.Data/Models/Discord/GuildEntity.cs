using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions.Actions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Data.Models.TimeTracking;
using Zhongli.Data.Models.VoiceChat;

namespace Zhongli.Data.Models.Discord;

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