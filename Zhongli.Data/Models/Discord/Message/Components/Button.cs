using System.ComponentModel.DataAnnotations;
using Discord;
using Zhongli.Data.Models.Discord.Message.Linking;

namespace Zhongli.Data.Models.Discord.Message.Components;

public class Button : Component
{
    protected Button() { }

    public Button(ButtonComponent button)
    {
        IsDisabled = button.IsDisabled;
        CustomId   = button.CustomId;
        Style      = button.Style;
        Emote      = button.Emote?.ToString();
        Label      = button.Label;
        Url        = button.Url;
    }

    /// <inheritdoc cref="ButtonComponent.Style" />
    public ButtonStyle Style { get; set; }

    public virtual LinkedButton? Link { get; set; }

    /// <inheritdoc cref="ButtonComponent.Emote" />
    public string? Emote { get; init; }

    /// <inheritdoc cref="ButtonComponent.Label" />
    [MaxLength(80)]
    public string? Label { get; init; }

    /// <inheritdoc cref="ButtonComponent.Url" />
    public string? Url { get; init; }

    public static implicit operator Button(ButtonComponent row) => new(row);

    public static implicit operator Button(ButtonBuilder builder) => new(builder.Build());
}