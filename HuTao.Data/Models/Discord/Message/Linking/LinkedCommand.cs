using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;

namespace HuTao.Data.Models.Discord.Message.Linking;

public class LinkedCommand
{
    protected LinkedCommand() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public LinkedCommand(string name, ILinkedCommandOptions options)
    {
        Name          = name;
        Ephemeral     = options.Ephemeral;
        Silent        = options.Silent;
        Authorization = options.ToAuthorizationGroups().ToList();
        Roles         = options.RoleTemplates.ToList();
        Message       = options.Message is not null ? new MessageTemplate(options.Message, options) : null;
        Description   = options.Description;
        Cooldown      = options.Cooldown;
        UserOptions   = options.UserOptions;
    }

    public Guid Id { get; set; }

    public bool Ephemeral { get; set; }

    public bool Silent { get; set; }

    public virtual ICollection<AuthorizationGroup> Authorization { get; set; }
        = new List<AuthorizationGroup>();

    public virtual ICollection<RoleTemplate> Roles { get; set; }
        = new List<RoleTemplate>();

    public virtual MessageTemplate? Message { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public TimeSpan? Cooldown { get; set; }

    public UserTargetOptions UserOptions { get; set; }
}