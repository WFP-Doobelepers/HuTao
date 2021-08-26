using Discord;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria
{
    public class RoleCriterion : Criterion, IRoleEntity
    {
        protected RoleCriterion() { }

        public RoleCriterion(IRole role)
        {
            RoleId  = role.Id;
            GuildId = role.Guild.Id;
        }

        public ulong GuildId { get; set; }

        public ulong RoleId { get; set; }

        public override string ToString() => this.MentionRole();
    }
}