using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria
{
    public class RoleCriterion : Criterion, IRoleEntity
    {
        protected RoleCriterion() { }

        public RoleCriterion(ulong roleId) { RoleId = roleId; }

        public ulong RoleId { get; set; }

        public override string ToString() => $"<@&{RoleId}>";
    }
}