using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Models.Discord;

namespace HuTao.Services.Interactive.Paginator;

public static class InteractiveExtensions
{
    public static IEnumerable<PageBuilder> ToPageBuilders(this EmbedBuilder builder, int chunk)
        => builder.Fields.ToList().ToPageBuilders(chunk, builder);

    public static IEnumerable<PageBuilder> ToPageBuilders(this IEnumerable<EmbedFieldBuilder> fields, int chunk,
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