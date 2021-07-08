using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria
{
    public class PermissionCriterion : Criterion, IPermissionEntity
    {
        protected PermissionCriterion() { }

        public PermissionCriterion(GuildPermission permission) { Permission = permission; }

        public GuildPermission Permission { get; set; }
    }
}