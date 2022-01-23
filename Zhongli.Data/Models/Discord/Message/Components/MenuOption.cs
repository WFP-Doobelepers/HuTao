using System;
using System.ComponentModel.DataAnnotations;
using Discord;

namespace Zhongli.Data.Models.Discord.Message.Components;

public class MenuOption
{
    protected MenuOption() { }

    public MenuOption(SelectMenuOption option)
    {
        IsDefault   = option.IsDefault;
        Description = option.Description;
        Emote       = option.Emote?.ToString();
        Label       = option.Label;
        Value       = option.Value;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="SelectMenuOption.IsDefault" />
    public bool? IsDefault { get; set; }

    /// <inheritdoc cref="SelectMenuOption.Label" />
    [MaxLength(100)]
    public string Label { get; set; } = null!;

    /// <inheritdoc cref="SelectMenuOption.Value" />
    [MaxLength(100)]
    public string Value { get; set; } = null!;

    /// <inheritdoc cref="SelectMenuOption.Description" />
    [MaxLength(100)]
    public string? Description { get; set; }

    /// <inheritdoc cref="SelectMenuOption.Emote" />
    public string? Emote { get; set; }

    public static implicit operator MenuOption(SelectMenuOption row) => new(row);

    public static implicit operator MenuOption(SelectMenuOptionBuilder builder) => new(builder.Build());
}