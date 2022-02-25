using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public class LinkedCommand
{
    protected LinkedCommand() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public LinkedCommand(string name, ILinkedCommandOptions options)
    {
        Name      = name;
        Scope     = options.Scope;
        Ephemeral = options.Ephemeral;
        Silent    = options.Silent;
        Message   = options.Message is not null ? new MessageTemplate(options.Message, options) : null;
        Cooldown  = options.Cooldown;
    }

    public Guid Id { get; set; }

    public AuthorizationScope Scope { get; set; }

    public bool Ephemeral { get; set; }

    public bool Silent { get; set; }

    public virtual ICollection<Criterion> Inclusions { get; set; }
        = new List<Criterion>();

    public virtual ICollection<RoleTemplate> Roles { get; set; }
        = new List<RoleTemplate>();

    public virtual MessageTemplate? Message { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public TimeSpan? Cooldown { get; set; }

    public UserTargetOptions UserOptions { get; set; }
}