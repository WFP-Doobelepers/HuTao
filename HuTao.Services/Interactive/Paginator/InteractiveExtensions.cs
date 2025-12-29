using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Services.Utilities;

namespace HuTao.Services.Interactive.Paginator;

public static class InteractiveExtensions
{
    public static IEnumerable<PageBuilder> ToPageBuilders(this EmbedBuilder builder, int chunk)
        => builder.Fields.ToList().ToPageBuilders(chunk, builder);

    public static IEnumerable<PageBuilder> ToPageBuilders(
        this IEnumerable<EmbedFieldBuilder> fields, int chunk,
        EmbedBuilder? builder = null)
        => fields.Chunk(chunk).Select(f => f.ToPageBuilder(builder));

    public static PageBuilder ToPageBuilder(this IEnumerable<EmbedFieldBuilder> fields, EmbedBuilder? builder = null)
    {
        builder        ??= new EmbedBuilder();
        builder.Fields =   fields.ToList();

        return PageBuilder.FromEmbed(builder.Build());
    }

    public static StaticPaginatorBuilder CreateDefaultPaginator() => new StaticPaginatorBuilder()
        .WithInputType(InputType.Buttons)
        .WithActionOnTimeout(ActionOnStop.DisableInput)
        .WithActionOnCancellation(ActionOnStop.DisableInput);

    /// <summary>
    /// Creates a default ComponentPaginator builder for V2 paginators with interactive components.
    /// Use this for paginators that need custom buttons, state management, or ComponentsV2 features.
    /// </summary>
    public static ComponentPaginatorBuilder CreateDefaultComponentPaginator() => new ComponentPaginatorBuilder()
        .WithActionOnTimeout(ActionOnStop.DisableInput)
        .WithActionOnCancellation(ActionOnStop.DisableInput);

    public static async Task<T?> TryFindEntityV2Async<T>(
        this InteractiveService service,
        Context context,
        IEnumerable<T> collection,
        Func<T, EmbedBuilder> builder,
        Func<T, string> id,
        string find,
        bool ephemeral = false,
        TimeSpan? timeout = null) where T : class
    {
        if (find.Length < 2)
            return null;

        var filtered = collection
            .Where(e => IsMatch(e, find))
            .Take(10)
            .ToList();

        if (filtered.Count <= 1)
            return filtered.Count == 1 ? filtered.First() : null;

        timeout ??= TimeSpan.FromMinutes(2);

        var selectionId = Guid.NewGuid();
        var components = BuildSelectionComponents(disabled: false);

        await context.ReplyAsync(components: components, ephemeral: ephemeral);

        var selected = await service.NextMessageComponentAsync(c =>
            c.User.Id == context.User.Id &&
            (c.Data.CustomId.StartsWith($"entity-select:{selectionId}:", StringComparison.Ordinal) ||
             c.Data.CustomId == $"entity-cancel:{selectionId}"), timeout: timeout.Value);

        if (selected.Value is not null)
        {
            _ = selected.Value.DeferAsync();
            _ = selected.Value.ModifyOriginalResponseAsync(m => m.Components = BuildSelectionComponents(disabled: true));
        }

        if (!selected.IsSuccess || selected.Value is null)
            return null;

        if (selected.Value.Data.CustomId == $"entity-cancel:{selectionId}")
            return null;

        var parts = selected.Value.Data.CustomId.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !int.TryParse(parts[^1], out var index))
            return null;

        return index >= 0 && index < filtered.Count ? filtered[index] : null;

        bool IsMatch(T entity, string match)
            => id(entity).StartsWith(match, StringComparison.OrdinalIgnoreCase);

        MessageComponent BuildSelectionComponents(bool disabled)
        {
            const uint accentColor = 0x9B59FF;
            const int maxTitleLength = 70;
            const int maxDescriptionLength = 160;

            var container = new ContainerBuilder()
                .WithTextDisplay($"## Multiple matches found ({filtered.Count})\nSelect the one you meant:")
                .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

            for (var i = 0; i < filtered.Count; i++)
            {
                var entity = filtered[i];
                var embed = builder(entity).Build();
                var entityId = id(entity);

                var title = (embed.Title ?? entityId).Truncate(maxTitleLength);
                var description = (embed.Description ?? string.Empty).Truncate(maxDescriptionLength);

                var text = new StringBuilder()
                    .AppendLine($"### {i + 1}. {title}")
                    .AppendLine(string.IsNullOrWhiteSpace(description) ? $"-# `{entityId}`" : description)
                    .AppendLine(string.IsNullOrWhiteSpace(description) ? string.Empty : $"-# `{entityId}`")
                    .ToString()
                    .Trim();

                var section = new SectionBuilder().WithTextDisplay(text);
                var thumbUrl = embed.Thumbnail?.Url;
                if (!string.IsNullOrWhiteSpace(thumbUrl))
                    section.WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(thumbUrl)));

                container.WithSection(section);

                if (i < filtered.Count - 1)
                    container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
            }

            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay("-# Tip: start typing the ID to narrow results • Click a number below to select")
                .WithAccentColor(accentColor);

            var componentBuilder = new ComponentBuilderV2().WithContainer(container);

            var buttons = filtered
                .Select((_, index) => new ButtonBuilder(
                    (index + 1).ToString(),
                    $"entity-select:{selectionId}:{index}",
                    ButtonStyle.Primary,
                    isDisabled: disabled))
                .ToList();

            foreach (var chunk in buttons.Chunk(5))
            {
                componentBuilder.WithActionRow(new ActionRowBuilder().WithComponents(chunk));
            }

            componentBuilder.WithActionRow(new ActionRowBuilder()
                .WithButton(new ButtonBuilder("Cancel", $"entity-cancel:{selectionId}", ButtonStyle.Danger, isDisabled: disabled)));

            return componentBuilder.Build();
        }
    }

    public static async Task<T?> TryFindEntityAsync<T>(
        this InteractiveService service, Context context, IEnumerable<T> collection,
        Func<T, EmbedBuilder> builder, Func<T, string> id, string find) where T : class
    {
        if (find.Length < 2)
            return null;

        var filtered = collection
            .Where(e => IsMatch(e, find))
            .Take(10).ToList();

        if (filtered.Count <= 1)
            return filtered.Count == 1 ? filtered.First() : null;

        var embeds = filtered.Zip(filtered.Select(builder).Select(e => e.Build())).ToList();
        var options = embeds.Select(f =>
        {
            var embed = f.Second;
            return new SelectMenuOptionBuilder()
                .WithValue(id(f.First))
                .WithLabel(embed.Title.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength))
                .WithDescription(embed.Description.Truncate(SelectMenuOptionBuilder.MaxDescriptionLength));
        }).ToList();

        var guid = Guid.NewGuid();
        var component = new ComponentBuilder()
            .WithSelectMenu($"select:{guid}", options, "Multiple matches found, select one...")
            .WithButton("Cancel selection", $"cancel:{guid}", ButtonStyle.Danger, new Emoji("🛑"));

        await context.ReplyAsync(embeds: embeds.Select(e => e.Second).ToArray(), components: component.Build());
        var selected = await service.NextMessageComponentAsync(c
            => c.Data.CustomId == $"select:{guid}"
            || c.Data.CustomId == $"cancel:{guid}");

        _ = selected.Value?.DeferAsync();
        _ = selected.Value?.ModifyOriginalResponseAsync(m => m.Components = new ComponentBuilder().Build());
        return selected.IsSuccess && selected.Value.Data.CustomId == $"select:{guid}"
            ? filtered.FirstOrDefault(e => IsMatch(e, selected.Value.Data.Values.First()))
            : null;

        bool IsMatch(T entity, string match) => id(entity).StartsWith(match, StringComparison.OrdinalIgnoreCase);
    }
}