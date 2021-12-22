using System.Collections.Generic;
using System.Linq;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace Zhongli.Services.Interactive.Paginator;

public static class InteractiveExtensions
{
    public static IEnumerable<PageBuilder> ToPageBuilders(this EmbedBuilder builder, int chunk)
        => builder.Fields.ToList().ToPageBuilders(chunk, builder);

    public static IEnumerable<PageBuilder> ToPageBuilders(this IEnumerable<EmbedFieldBuilder> fields, int chunk, EmbedBuilder? builder = null)
        => fields.Chunk(chunk).Select(f => f.ToPageBuilder(builder));

    public static PageBuilder ToPageBuilder(this IEnumerable<EmbedFieldBuilder> fields, EmbedBuilder? builder = null)
    {
        builder        ??= new EmbedBuilder();
        builder.Fields =   fields.ToList();

        return PageBuilder.FromEmbed(builder.Build());
    }

    public static StaticPaginatorBuilder CreateDefaultPaginator() => new StaticPaginatorBuilder()
        .WithDefaultEmotes()
        .WithInputType(InputType.Buttons)
        .WithActionOnTimeout(ActionOnStop.DeleteInput)
        .WithActionOnCancellation(ActionOnStop.DeleteInput);
}