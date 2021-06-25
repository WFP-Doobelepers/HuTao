using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Reprimands;
using Zhongli.Data.Models.VoiceChat;

namespace Zhongli.Data.Models.Discord
{
    public class GuildEntity
    {
        protected GuildEntity() { }

        public GuildEntity(ulong id) { Id = id; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public virtual AuthorizationRules? AuthorizationRules { get; set; }

        public virtual AutoModerationRules? AutoModerationRules { get; set; }

        public virtual ICollection<Ban> BanHistory { get; init; } = new List<Ban>();

        public virtual ICollection<GuildUserEntity> GuildUsers { get; init; } = new List<GuildUserEntity>();

        public virtual ICollection<Kick> KickHistory { get; init; } = new List<Kick>();

        public virtual ICollection<Mute> MuteHistory { get; init; } = new List<Mute>();

        public virtual ICollection<Warning> WarningHistory { get; init; } = new List<Warning>();

        public ulong? MuteRoleId { get; set; }

        public virtual VoiceChatRules? VoiceChatRules { get; set; }
    }
}