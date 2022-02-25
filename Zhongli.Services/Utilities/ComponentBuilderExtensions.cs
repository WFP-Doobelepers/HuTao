using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Zhongli.Data.Models.Discord.Message.Components;

namespace Zhongli.Services.Utilities;

public static class ComponentBuilderExtensions
{
    private const int MaxActionRowCount = 5;
    private const int MaxComponentsCount = 5;

    public static ComponentBuilder ToBuilder(this IEnumerable<ActionRow> rows)
    {
        var builders = rows
            .Select(r => r.Components.Select(c => c.ToComponent()))
            .Select(c => new ActionRowBuilder().WithComponents(c.ToList()));

        return new ComponentBuilder().WithRows(builders);
    }

    public static ICollection<ActionRow> AddComponent(this ICollection<ActionRow> rows, Component component,
        int row = 0)
    {
        if (row > MaxActionRowCount)
            throw new ArgumentOutOfRangeException(nameof(row), row, $"There can only be {MaxActionRowCount} rows.");
        var x = ComponentBuilder.MaxActionRowCount * ActionRowBuilder.MaxChildCount;
        var actionRow = rows.ElementAtOrDefault(row) ?? rows.Insert(new ActionRow());

        if (actionRow.CanTakeComponent(component))
            actionRow.AddComponent(component);
        else if (row < MaxActionRowCount)
            rows.AddComponent(component, row + 1);
        else
            throw new InvalidOperationException($"There is no more row to add this {nameof(component)}.");

        return rows;
    }

    private static bool CanTakeComponent(this ActionRow row, Component component) => component switch
    {
        Button     => row.Components.All(c => c is not SelectMenu) && row.Components.Count < MaxComponentsCount,
        SelectMenu => row.Components.Count is 0,
        _          => false
    };

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

    private static void AddComponent(this ActionRow row, Component component)
    {
        if (row.Components.Count >= MaxComponentsCount)
            throw new InvalidOperationException($"Components count reached {MaxComponentsCount}");

        row.Components.Insert(component);
    }
}