using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace Zhongli.Data.Models.Discord.Message.Components;

public class SelectMenu : Component
{
    protected SelectMenu() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public SelectMenu(SelectMenuComponent menu)
    {
        IsDisabled  = menu.IsDisabled;
        CustomId    = menu.CustomId;
        Options     = menu.Options.Select(o => new MenuOption(o)).ToList();
        MaxValues   = menu.MaxValues;
        MinValues   = menu.MinValues;
        Placeholder = menu.Placeholder;
    }

    public virtual ICollection<MenuOption> Options { get; set; }
        = new List<MenuOption>();

    /// <inheritdoc cref="SelectMenuComponent.MaxValues" />
    public int MaxValues { get; set; }

    /// <inheritdoc cref="SelectMenuComponent.MinValues" />
    public int MinValues { get; set; }

    /// <inheritdoc cref="SelectMenuComponent.Placeholder" />
    [MaxLength(100)]
    public string? Placeholder { get; set; }
}