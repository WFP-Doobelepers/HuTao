using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class RoleMentionExclusion : ModerationExclusion, IRoleEntity, IJudge<ulong>
{
    protected RoleMentionExclusion() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public RoleMentionExclusion(RoleEntity role, AutoConfiguration? config) : base(config) { Role = role; }

    public virtual RoleEntity Role { get; set; } = null!;

    [Column(nameof(GuildId))]
    public ulong GuildId { get; set; }

    public bool Judge(ulong roleId) => RoleId == roleId;

    public ulong RoleId { get; set; }
}