using Humanizer;
using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Criteria;

public class PermissionCriterion : Criterion, IPermissionEntity
{
    protected PermissionCriterion() { }

    public PermissionCriterion(GuildPermission permission) { Permission = permission; }

    public GuildPermission Permission { get; set; }

    public override string ToString() => Permission.Humanize();
}