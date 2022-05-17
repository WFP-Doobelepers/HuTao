using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;

namespace HuTao.Data.Models.Discord;

public class RoleEntity : IRoleEntity
{
    protected RoleEntity() { }

    public RoleEntity(ulong guildId, ulong roleId)
    {
        GuildId = guildId;
        RoleId  = roleId;
    }

    public RoleEntity(IRole role) : this(role.Guild.Id, role.Id) { }

    public virtual ICollection<HardMute> Mutes { get; set; } = new List<HardMute>();

    public ulong GuildId { get; set; }

    [Key] public ulong RoleId { get; set; }
}