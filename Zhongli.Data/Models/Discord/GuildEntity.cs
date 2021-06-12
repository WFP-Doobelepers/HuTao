using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;

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

        public virtual ICollection<GuildUserEntity> GuildUsers { get; set; }

        public virtual ICollection<Warning> Warnings { get; set; }
    }
}