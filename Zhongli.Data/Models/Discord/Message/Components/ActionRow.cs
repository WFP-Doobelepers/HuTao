using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace Zhongli.Data.Models.Discord.Message.Components;

public class ActionRow
{
    protected ActionRow() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ActionRow(ActionRowComponent row)
    {
        Components = row.Components
            .Select<IMessageComponent, Component>(c => c switch
            {
                ButtonComponent button   => new Button(button),
                SelectMenuComponent menu => new SelectMenu(menu),
                _                        => throw new ArgumentOutOfRangeException(nameof(c))
            })
            .ToList();
    }

    public Guid Id { get; set; }

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();
}