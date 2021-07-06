using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.VoiceChat;

namespace Zhongli.Data.Models.Discord
{
    public class GuildEntity
    {
        protected GuildEntity() { }

        public GuildEntity(ulong id) { Id = id; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public virtual ICollection<AuthorizationGroup> AuthorizationGroups { get; set; }

        public virtual AutoModerationRules? AutoModerationRules { get; set; }

        public ulong? MuteRoleId { get; set; }

        public virtual VoiceChatRules? VoiceChatRules { get; set; }

        public virtual LoggingRules? LoggingRules { get; set; }
    }
}