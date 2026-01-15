using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Xunit;

namespace HuTao.Tests.Testing;

public static class ComponentsV2Assertions
{
    private const int MaxTextDisplayLength = 4000;
    private const int MaxThumbnailDescriptionLength = 1024;
    private const int MaxMediaGalleryItemDescriptionLength = 256;
    private const int MaxMediaGalleryItems = 10;
    private const int MaxActionRows = 5;
    private const int MaxActionRowComponents = 5;
    private const int MaxSectionTextDisplays = 3;
    private const int MaxButtonLabelLength = 80;
    private const int MaxCustomIdLength = 100;
    private const int MaxPlaceholderLength = 150;
    private const int MaxSelectMenuOptions = 25;

    public static void ShouldBeValidComponentsV2(this MessageComponent components)
    {
        var violations = ValidateComponentsV2(components);
        if (violations.Count == 0)
            return;

        Assert.Fail(string.Join(Environment.NewLine, violations));
    }

    private static List<string> ValidateComponentsV2(MessageComponent components)
    {
        var violations = new List<string>();
        var actionRowCount = 0;

        VisitMany(components.Components, "root");

        if (actionRowCount > MaxActionRows)
            violations.Add($"Too many action rows: {actionRowCount} (max {MaxActionRows}).");

        return violations;

        void VisitMany(IEnumerable<IMessageComponent> list, string path)
        {
            foreach (var component in list)
            {
                if (component is null)
                {
                    violations.Add($"{path}: component is null.");
                    continue;
                }

                var nextPath = $"{path}/{component.GetType().Name}";

                switch (component)
                {
                    case ContainerComponent container:
                        VisitMany(container.Components, nextPath);
                        break;

                    case SectionComponent section:
                        ValidateSection(section, nextPath);
                        break;

                    case ActionRowComponent actionRow:
                        actionRowCount++;
                        ValidateActionRow(actionRow, nextPath);
                        break;

                    case TextDisplayComponent textDisplay:
                        ValidateTextDisplay(textDisplay, nextPath);
                        break;

                    case ThumbnailComponent thumbnail:
                        ValidateThumbnail(thumbnail, nextPath);
                        break;

                    case MediaGalleryComponent mediaGallery:
                        ValidateMediaGallery(mediaGallery, nextPath);
                        break;

                    case FileComponent file:
                        ValidateFile(file, nextPath);
                        break;

                    case SeparatorComponent:
                        break;

                    default:
                        break;
                }
            }
        }

        void ValidateSection(SectionComponent section, string path)
        {
            if (section.Components.Count > MaxSectionTextDisplays)
                violations.Add($"{path}: too many section children: {section.Components.Count} (max {MaxSectionTextDisplays}).");

            foreach (var child in section.Components)
            {
                if (child is not TextDisplayComponent text)
                {
                    violations.Add($"{path}: invalid section child type {child.GetType().Name} (only TextDisplay allowed).");
                    continue;
                }

                ValidateTextDisplay(text, $"{path}/TextDisplayComponent");
            }

            if (section.Accessory is null)
                return;

            if (section.Accessory is ThumbnailComponent thumb)
            {
                ValidateThumbnail(thumb, $"{path}/Accessory/ThumbnailComponent");
                return;
            }

            if (section.Accessory is ButtonComponent button)
            {
                ValidateButton(button, $"{path}/Accessory/ButtonComponent");
                return;
            }

            violations.Add($"{path}: invalid accessory type {section.Accessory.GetType().Name} (expected ThumbnailComponent or ButtonComponent).");
        }

        void ValidateTextDisplay(TextDisplayComponent textDisplay, string path)
        {
            if (textDisplay.Content is null)
            {
                violations.Add($"{path}: TextDisplay content is null.");
                return;
            }

            if (textDisplay.Content.Length == 0)
                violations.Add($"{path}: TextDisplay content is empty.");

            if (textDisplay.Content.Length > MaxTextDisplayLength)
                violations.Add($"{path}: TextDisplay content too long: {textDisplay.Content.Length} (max {MaxTextDisplayLength}).");
        }

        void ValidateThumbnail(ThumbnailComponent thumbnail, string path)
        {
            if (thumbnail.Media.Url is null)
            {
                violations.Add($"{path}: thumbnail media URL is null.");
                return;
            }

            if (thumbnail.Media.Url.Length == 0)
                violations.Add($"{path}: thumbnail media URL is empty.");

            if (thumbnail.Description is { Length: > MaxThumbnailDescriptionLength })
                violations.Add($"{path}: thumbnail description too long: {thumbnail.Description.Length} (max {MaxThumbnailDescriptionLength}).");
        }

        void ValidateMediaGallery(MediaGalleryComponent gallery, string path)
        {
            if (gallery.Items.Count > MaxMediaGalleryItems)
                violations.Add($"{path}: too many media items: {gallery.Items.Count} (max {MaxMediaGalleryItems}).");

            for (var i = 0; i < gallery.Items.Count; i++)
            {
                var item = gallery.Items.ElementAt(i);

                if (item.Media.Url is null)
                {
                    violations.Add($"{path}[{i}]: media URL is null.");
                    continue;
                }

                if (item.Media.Url.Length == 0)
                    violations.Add($"{path}[{i}]: media URL is empty.");

                if (item.Description is { Length: > MaxMediaGalleryItemDescriptionLength })
                    violations.Add($"{path}[{i}]: media description too long: {item.Description.Length} (max {MaxMediaGalleryItemDescriptionLength}).");
            }
        }

        void ValidateFile(FileComponent file, string path)
        {
            if (file.File.Url is null)
            {
                violations.Add($"{path}: file URL is null.");
                return;
            }

            if (!file.File.Url.StartsWith("attachment://", StringComparison.Ordinal))
                violations.Add($"{path}: file URL must start with attachment:// (got {file.File.Url}).");
        }

        void ValidateActionRow(ActionRowComponent actionRow, string path)
        {
            if (actionRow.Components.Count > MaxActionRowComponents)
                violations.Add($"{path}: too many action row components: {actionRow.Components.Count} (max {MaxActionRowComponents}).");

            foreach (var child in actionRow.Components)
            {
                switch (child)
                {
                    case ButtonComponent button:
                        ValidateButton(button, $"{path}/ButtonComponent");
                        break;
                    case SelectMenuComponent menu:
                        ValidateSelectMenu(menu, $"{path}/SelectMenuComponent");
                        break;
                    default:
                        break;
                }
            }
        }

        void ValidateButton(ButtonComponent button, string path)
        {
            if (button.Label is { Length: > MaxButtonLabelLength })
                violations.Add($"{path}: button label too long: {button.Label.Length} (max {MaxButtonLabelLength}).");

            var isLink = button.Style == ButtonStyle.Link;
            if (isLink)
            {
                if (string.IsNullOrWhiteSpace(button.Url))
                    violations.Add($"{path}: link button URL is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(button.CustomId))
            {
                violations.Add($"{path}: non-link button missing custom ID.");
                return;
            }

            if (button.CustomId.Length > MaxCustomIdLength)
                violations.Add($"{path}: button custom ID too long: {button.CustomId.Length} (max {MaxCustomIdLength}).");
        }

        void ValidateSelectMenu(SelectMenuComponent menu, string path)
        {
            if (string.IsNullOrWhiteSpace(menu.CustomId))
            {
                violations.Add($"{path}: select menu missing custom ID.");
                return;
            }

            if (menu.CustomId.Length > MaxCustomIdLength)
                violations.Add($"{path}: select menu custom ID too long: {menu.CustomId.Length} (max {MaxCustomIdLength}).");

            if (menu.Placeholder is { Length: > MaxPlaceholderLength })
                violations.Add($"{path}: select menu placeholder too long: {menu.Placeholder.Length} (max {MaxPlaceholderLength}).");

            if (menu.Options is { Count: > MaxSelectMenuOptions })
                violations.Add($"{path}: too many select menu options: {menu.Options.Count} (max {MaxSelectMenuOptions}).");

            if (menu.MinValues < 0)
                violations.Add($"{path}: select menu MinValues must be >= 0.");

            if (menu.MaxValues < 1)
                violations.Add($"{path}: select menu MaxValues must be >= 1.");
        }
    }
}

