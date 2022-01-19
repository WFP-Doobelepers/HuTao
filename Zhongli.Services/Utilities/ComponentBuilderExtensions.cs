using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Zhongli.Data.Models.Discord.Message.Components;

namespace Zhongli.Services.Utilities;

public static class ComponentBuilderExtensions
{
    public static ComponentBuilder? ToBuilder(this ICollection<ActionRow> rows)
    {
        if (rows.Count is 0)
            return null;

        var builders = rows
            .Select(r => r.Components.Select(c => c.ToComponent()))
            .Select(c => new ActionRowBuilder().WithComponents(c.ToList()));

        return new ComponentBuilder().WithRows(builders);
    }

    private static ButtonBuilder ToBuilder(this Button button) => new()
    {
        Label      = button.Label,
        CustomId   = button.CustomId,
        Style      = button.Style,
        Emote      = button.Emote?.ToEmote(),
        Url        = button.Url,
        IsDisabled = button.IsDisabled
    };

    private static IEmote? ToEmote(this string? e) =>
        Emote.TryParse(e, out var emote) ? emote :
        Emoji.TryParse(e, out var emoji) ? emoji : null;

    private static IMessageComponent ToComponent(this Component component) => component switch
    {
        Button button   => button.ToBuilder().Build(),
        SelectMenu menu => menu.ToBuilder().Build(),
        _               => throw new ArgumentOutOfRangeException(nameof(component))
    };

    private static SelectMenuBuilder ToBuilder(this SelectMenu menu) => new()
    {
        CustomId    = menu.CustomId,
        IsDisabled  = menu.IsDisabled,
        MaxValues   = menu.MaxValues,
        MinValues   = menu.MaxValues,
        Options     = menu.Options.Select(ToBuilder).ToList(),
        Placeholder = menu.Placeholder
    };

    private static SelectMenuOptionBuilder ToBuilder(this MenuOption option) => new()
    {
        Description = option.Description,
        Emote       = option.Emote?.ToEmote(),
        IsDefault   = option.IsDefault,
        Label       = option.Label,
        Value       = option.Value
    };
}