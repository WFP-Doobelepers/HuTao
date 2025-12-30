using System.Text;
using Discord;
using Humanizer;

namespace HuTao.Services.Utilities;

public static class ComponentsV2EmbedExtensions
{
    private const int DefaultMaxChars = 550;
    private const uint DefaultAccentColor = 0x9B59FF;

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

    public static MessageComponent ToComponentsV2Message(
        this Embed embed,
        uint? accentColor = null,
        int maxChars = 3800,
        string? footer = null)
    {
        var container = new ContainerBuilder()
            .WithSection(embed.ToComponentsV2Section(maxChars))
            .WithAccentColor(accentColor ?? embed.Color?.RawValue ?? DefaultAccentColor);

        if (!string.IsNullOrWhiteSpace(footer))
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay(footer);
        }

        return new ComponentBuilderV2()
            .WithContainer(container)
            .Build();
    }
}

