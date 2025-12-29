using System.Text;
using Discord;
using Humanizer;

namespace HuTao.Services.Utilities;

public static class ComponentsV2EmbedExtensions
{
    private const int DefaultMaxChars = 550;

    public static string ToComponentsV2Text(this Embed embed, int maxChars = DefaultMaxChars)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(embed.Title))
            sb.AppendLine($"### {embed.Title}");

        if (!string.IsNullOrWhiteSpace(embed.Description))
            sb.AppendLine(embed.Description);

        foreach (var field in embed.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) && string.IsNullOrWhiteSpace(field.Value))
                continue;

            var name = string.IsNullOrWhiteSpace(field.Name) ? " " : field.Name;
            var value = string.IsNullOrWhiteSpace(field.Value) ? " " : field.Value;
            sb.AppendLine($"**{name}**: {value}");
        }

        var text = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(text) ? "-" : text.Truncate(maxChars);
    }

    public static SectionBuilder ToComponentsV2Section(this Embed embed, int maxChars = DefaultMaxChars)
    {
        var section = new SectionBuilder()
            .WithTextDisplay(embed.ToComponentsV2Text(maxChars));

        var thumbUrl = embed.Thumbnail?.Url;
        if (!string.IsNullOrWhiteSpace(thumbUrl))
        {
            section.WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(thumbUrl)));
        }

        return section;
    }
}

