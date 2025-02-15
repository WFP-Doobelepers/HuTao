using System.Collections.Generic;
using System.Linq;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Linking;

public interface IRoleTemplateOptions
{
    public IEnumerable<IRole>? AddRoles { get; }

    public IEnumerable<IRole>? RemoveRoles { get; }

    public IEnumerable<IRole>? ToggleRoles { get; }

    public IEnumerable<RoleTemplate> RoleTemplates
        => new List<RoleTemplate>()
            .Concat(GetRoleTemplate(AddRoles, RoleBehavior.Add))
            .Concat(GetRoleTemplate(RemoveRoles, RoleBehavior.Remove))
            .Concat(GetRoleTemplate(ToggleRoles, RoleBehavior.Toggle));

    private static IEnumerable<RoleTemplate> GetRoleTemplate(IEnumerable<IRole>? roles, RoleBehavior behavior)
        => roles?.Select(r => new RoleTemplate(r, behavior)) ?? [];
}