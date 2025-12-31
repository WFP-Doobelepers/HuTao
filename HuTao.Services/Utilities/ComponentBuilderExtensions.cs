using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Data.Models.Discord.Message.Components;

namespace HuTao.Services.Utilities;

public static class ComponentBuilderExtensions
{
    private const int MaxActionRowCount = 5;
    private const int MaxComponentsCount = 5;

    public static IEnumerable<ActionRowBuilder> ToActionRowBuilders(this IEnumerable<ActionRow> rows)
        => rows
            .Select(r => r.Components.Select(c => c.ToBuilder()))
            .Select(c => new ActionRowBuilder().WithComponents(c.ToList()));

    public static ICollection<ActionRow> AddComponent(
        this ICollection<ActionRow> rows, Component component,
        int row = 0)
    {
        if (row > MaxActionRowCount)
            throw new ArgumentOutOfRangeException(nameof(row), row, $"There can only be {MaxActionRowCount} rows.");
        var actionRow = rows.ElementAtOrDefault(row) ?? rows.Insert(new ActionRow());

        if (actionRow.CanTakeComponent(component))
            actionRow.AddComponent(component);
        else if (row < MaxActionRowCount)
            rows.AddComponent(component, row + 1);
        else
            throw new InvalidOperationException($"There is no more row to add this {nameof(component)}.");

        return rows;
    }

    public static ModalBuilder UpdateTextInput(this ModalBuilder modal, string customId, Action<TextInputBuilder> input)
    {
        var components = modal.Components.ActionRows.SelectMany(r => r.Components).OfType<TextInputBuilder>();
        var component = components.First(c => c.CustomId == customId);

        var builder = new TextInputBuilder
        {
            CustomId    = customId,
            Label       = component.Label,
            MaxLength   = component.MaxLength,
            MinLength   = component.MinLength,
            Placeholder = component.Placeholder,
            Style       = component.Style,
            Value       = component.Value
        };

        input(builder);

        foreach (var row in modal.Components.ActionRows.Where(row => row.Components.OfType<IInteractableComponentBuilder>().Any(c => c.CustomId == customId)))
        {
            row.Components.RemoveAll(c => c is IInteractableComponentBuilder interactable && interactable.CustomId == customId);
            row.AddComponent(builder);
        }

        return modal;
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

    private static IMessageComponentBuilder ToBuilder(this Component component) => component switch
    {
        Button button   => button.ToBuilder(),
        SelectMenu menu => menu.ToBuilder(),
        _               => throw new ArgumentOutOfRangeException(nameof(component))
    };

    private static SelectMenuBuilder ToBuilder(this SelectMenu menu) => new()
    {
        CustomId    = menu.CustomId,
        IsDisabled  = menu.IsDisabled,
        MaxValues   = menu.MaxValues,
        MinValues   = menu.MinValues,
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