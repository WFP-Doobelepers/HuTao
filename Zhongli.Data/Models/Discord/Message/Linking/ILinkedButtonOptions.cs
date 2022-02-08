using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public interface ILinkedButtonOptions : IMessageTemplateOptions
{
    public bool Ephemeral { get; set; }

    public bool IsDisabled { get; set; }

    public ButtonStyle Style { get; set; }

    public IEmote? Emote { get; set; }

    public IEnumerable<IRole>? AddRoles { get; set; }

    public IEnumerable<IRole>? RemoveRoles { get; set; }

    public IEnumerable<IRole>? ToggleRoles { get; set; }

    public IEnumerable<RoleTemplate> RoleTemplates
        => new List<RoleTemplate>()
            .Concat(GetRoleTemplate(r => r.AddRoles, RoleBehavior.Add))
            .Concat(GetRoleTemplate(r => r.RemoveRoles, RoleBehavior.Remove))
            .Concat(GetRoleTemplate(r => r.ToggleRoles, RoleBehavior.Toggle));

    public IMessage? Message { get; set; }

    public int Row { get; set; }

    public MessageTemplate? MessageTemplate
        => Message is null ? null : new MessageTemplate(Message, this);

    public string? Label { get; set; }

    public string? Url { get; set; }

    private IEnumerable<RoleTemplate> GetRoleTemplate(
        Func<ILinkedButtonOptions, IEnumerable<IRole>?> selector, RoleBehavior behavior)
        => selector(this)?.Select(r => new RoleTemplate(r, behavior)) ?? Enumerable.Empty<RoleTemplate>();
}