using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public class RoleTemplate : IRoleEntity
{
    protected RoleTemplate() { }

    public RoleTemplate(IRole role, RoleBehavior behavior)
    {
        RoleId   = role.Id;
        GuildId  = role.Guild.Id;
        Behavior = behavior;
    }

    public Guid Id { get; set; }

    public RoleBehavior Behavior { get; set; }

    public ulong GuildId { get; set; }

    public ulong RoleId { get; set; }
}