using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Components;

public class ActionRow
{
    public ActionRow() { }

    public ActionRow(ActionRowComponent row) : this(row.Components) { }

    public ActionRow(ActionRowBuilder row) : this(row.Components) { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    private ActionRow(IEnumerable<IMessageComponent> components)
    {
        Components = components
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

    public static implicit operator ActionRow(ActionRowComponent row) => new(row);

    public static implicit operator ActionRow(ActionRowBuilder builder) => new(builder);
}