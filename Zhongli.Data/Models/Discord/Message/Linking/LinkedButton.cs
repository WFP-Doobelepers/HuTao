using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zhongli.Data.Models.Discord.Message.Components;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public class LinkedButton
{
    protected LinkedButton() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public LinkedButton(Button button, ILinkedButtonOptions options)
    {
        Button    = button;
        Message   = options.MessageTemplate;
        Roles     = options.RoleTemplates.ToList();
        Ephemeral = options.Ephemeral;
    }

    public Guid ButtonId { get; set; }

    public Guid Id { get; set; }

    public bool Ephemeral { get; set; }

    public virtual Button Button { get; set; } = null!;

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual ICollection<RoleTemplate> Roles { get; set; }
        = new List<RoleTemplate>();

    public virtual MessageTemplate? Message { get; set; }
}