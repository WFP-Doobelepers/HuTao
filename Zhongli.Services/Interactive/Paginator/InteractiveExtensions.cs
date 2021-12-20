using System.Collections.Generic;
using System.Linq;
using Discord;
using Interactivity;

namespace Zhongli.Services.Interactive.Paginator;

public static class InteractiveExtensions
{
    public static IEnumerable<PageBuilder> ToPageBuilders(this EmbedBuilder builder, int chunk)
        => builder.Fields.Chunk(chunk).Select(f => f.ToPageBuilder(builder));

    public static IEnumerable<PageBuilder> ToPageBuilders(this IEnumerable<EmbedFieldBuilder> fields, int chunk, EmbedBuilder? builder = null)
        => fields.Chunk(chunk).Select(p => ToPageBuilder(p, builder));

    public static PageBuilder ToPageBuilder(this IEnumerable<EmbedFieldBuilder> fields, EmbedBuilder? builder = null)
    {
        builder ??= new EmbedBuilder();
        builder.Fields = fields.ToList();

        return PageBuilder.FromEmbedBuilder(builder);
    }
}