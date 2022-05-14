using System;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Linking;

public record RoleMetadata(RoleTemplate Template, IGuildUser User)
{
    public IRole Role => User.Guild.GetRole(Template.RoleId);
}

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

    public override string ToString() => $"{Behavior} {this.MentionRole()}";
}