using System;
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
            .Concat(GetRoleTemplate(r => r.AddRoles, RoleBehavior.Add))
            .Concat(GetRoleTemplate(r => r.RemoveRoles, RoleBehavior.Remove))
            .Concat(GetRoleTemplate(r => r.ToggleRoles, RoleBehavior.Toggle));

    private IEnumerable<RoleTemplate> GetRoleTemplate(
        Func<IRoleTemplateOptions, IEnumerable<IRole>?> selector, RoleBehavior behavior)
        => selector(this)?.Select(r => new RoleTemplate(r, behavior)) ?? Enumerable.Empty<RoleTemplate>();
}