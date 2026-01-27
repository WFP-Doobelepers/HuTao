using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace HuTao.Services.Utilities;

public static class ComponentsV2Validator
{
    public const int MaxTotalComponents = 40;
    public const int MaxCumulativeTextLength = 4000;
    public const int MaxActionRows = 5;
    public const int MaxActionRowComponents = 5;
    public const int MaxSectionTextDisplays = 3;
    public const int MaxTextDisplayLength = 4000;
    public const int MaxThumbnailDescriptionLength = 1024;
    public const int MaxMediaGalleryItems = 10;
    public const int MaxMediaGalleryItemDescriptionLength = 256;
    public const int MaxButtonLabelLength = 80;
    public const int MaxCustomIdLength = 100;
    public const int MaxPlaceholderLength = 150;
    public const int MaxSelectMenuOptions = 25;

    public static ValidationResult Validate(MessageComponent components)
    {
        var result = new ValidationResult();
        var context = new ValidationContext();

        VisitComponents(components.Components, "root", context, result);

        if (context.TotalComponentCount > MaxTotalComponents)
            result.AddViolation($"Total component count {context.TotalComponentCount} exceeds maximum {MaxTotalComponents}.");

        if (context.ActionRowCount > MaxActionRows)
            result.AddViolation($"Total action row count {context.ActionRowCount} exceeds maximum {MaxActionRows}.");

        if (context.CumulativeTextLength > MaxCumulativeTextLength)
            result.AddViolation($"Cumulative text length {context.CumulativeTextLength} exceeds maximum {MaxCumulativeTextLength}.");

        return result;
    }

    public static int CountAllComponents(MessageComponent components)
    {
        var count = 0;
        CountComponentsRecursive(components.Components, ref count);
        return count;
    }

    public static int CountAllComponents(IEnumerable<IMessageComponent> components)
    {
        var count = 0;
        CountComponentsRecursive(components, ref count);
        return count;
    }

    private static void CountComponentsRecursive(IEnumerable<IMessageComponent> components, ref int count)
    {
        foreach (var component in components)
        {
            count++;
            switch (component)
            {
                case ContainerComponent container:
                    CountComponentsRecursive(container.Components, ref count);
                    break;
                case SectionComponent section:
                    CountComponentsRecursive(section.Components, ref count);
                    if (section.Accessory is not null)
                        count++;
                    break;
                case ActionRowComponent actionRow:
                    CountComponentsRecursive(actionRow.Components, ref count);
                    break;
            }
        }
    }

    private static void VisitComponents(
        IEnumerable<IMessageComponent> components,
        string path,
        ValidationContext context,
        ValidationResult result)
    {
        foreach (var component in components)
        {
            if (component is null)
            {
                result.AddViolation($"{path}: component is null.");
                continue;
            }

            context.TotalComponentCount++;
            var nextPath = $"{path}/{component.GetType().Name}";

            switch (component)
            {
                case ContainerComponent container:
                    VisitComponents(container.Components, nextPath, context, result);
                    break;

                case SectionComponent section:
                    ValidateSection(section, nextPath, context, result);
                    break;

                case ActionRowComponent actionRow:
                    context.ActionRowCount++;
                    ValidateActionRow(actionRow, nextPath, context, result);
                    break;

                case TextDisplayComponent textDisplay:
                    ValidateTextDisplay(textDisplay, nextPath, context, result);
                    break;

                case ThumbnailComponent thumbnail:
                    ValidateThumbnail(thumbnail, nextPath, result);
                    break;

                case MediaGalleryComponent mediaGallery:
                    ValidateMediaGallery(mediaGallery, nextPath, result);
                    break;

                case FileComponent file:
                    ValidateFile(file, nextPath, result);
                    break;

                case SeparatorComponent:
                    break;
            }
        }
    }

    private static void ValidateSection(
        SectionComponent section,
        string path,
        ValidationContext context,
        ValidationResult result)
    {
        if (section.Components.Count > MaxSectionTextDisplays)
            result.AddViolation($"{path}: too many section children: {section.Components.Count} (max {MaxSectionTextDisplays}).");

        foreach (var child in section.Components)
        {
            context.TotalComponentCount++;

            if (child is not TextDisplayComponent text)
            {
                result.AddViolation($"{path}: invalid section child type {child.GetType().Name} (only TextDisplay allowed).");
                continue;
            }

            ValidateTextDisplay(text, $"{path}/TextDisplayComponent", context, result);
        }

        if (section.Accessory is null)
            return;

        context.TotalComponentCount++;

        switch (section.Accessory)
        {
            case ThumbnailComponent thumb:
                ValidateThumbnail(thumb, $"{path}/Accessory/ThumbnailComponent", result);
                break;
            case ButtonComponent button:
                ValidateButton(button, $"{path}/Accessory/ButtonComponent", result);
                break;
            default:
                result.AddViolation($"{path}: invalid accessory type {section.Accessory.GetType().Name} (expected ThumbnailComponent or ButtonComponent).");
                break;
        }
    }

    private static void ValidateTextDisplay(
        TextDisplayComponent textDisplay,
        string path,
        ValidationContext context,
        ValidationResult result)
    {
        if (textDisplay.Content is null)
        {
            result.AddViolation($"{path}: TextDisplay content is null.");
            return;
        }

        if (textDisplay.Content.Length == 0)
            result.AddViolation($"{path}: TextDisplay content is empty.");

        if (textDisplay.Content.Length > MaxTextDisplayLength)
            result.AddViolation($"{path}: TextDisplay content too long: {textDisplay.Content.Length} (max {MaxTextDisplayLength}).");

        context.CumulativeTextLength += textDisplay.Content.Length;
    }

    private static void ValidateThumbnail(ThumbnailComponent thumbnail, string path, ValidationResult result)
    {
        if (thumbnail.Media.Url is null)
        {
            result.AddViolation($"{path}: thumbnail media URL is null.");
            return;
        }

        if (thumbnail.Media.Url.Length == 0)
            result.AddViolation($"{path}: thumbnail media URL is empty.");

        if (thumbnail.Description is { Length: > MaxThumbnailDescriptionLength })
            result.AddViolation($"{path}: thumbnail description too long: {thumbnail.Description.Length} (max {MaxThumbnailDescriptionLength}).");
    }

    private static void ValidateMediaGallery(MediaGalleryComponent gallery, string path, ValidationResult result)
    {
        if (gallery.Items.Count > MaxMediaGalleryItems)
            result.AddViolation($"{path}: too many media items: {gallery.Items.Count} (max {MaxMediaGalleryItems}).");

        for (var i = 0; i < gallery.Items.Count; i++)
        {
            var item = gallery.Items.ElementAt(i);

            if (item.Media.Url is null)
            {
                result.AddViolation($"{path}[{i}]: media URL is null.");
                continue;
            }

            if (item.Media.Url.Length == 0)
                result.AddViolation($"{path}[{i}]: media URL is empty.");

            if (item.Description is { Length: > MaxMediaGalleryItemDescriptionLength })
                result.AddViolation($"{path}[{i}]: media description too long: {item.Description.Length} (max {MaxMediaGalleryItemDescriptionLength}).");
        }
    }

    private static void ValidateFile(FileComponent file, string path, ValidationResult result)
    {
        if (file.File.Url is null)
        {
            result.AddViolation($"{path}: file URL is null.");
            return;
        }

        if (!file.File.Url.StartsWith("attachment://", StringComparison.Ordinal))
            result.AddViolation($"{path}: file URL must start with attachment:// (got {file.File.Url}).");
    }

    private static void ValidateActionRow(
        ActionRowComponent actionRow,
        string path,
        ValidationContext context,
        ValidationResult result)
    {
        if (actionRow.Components.Count > MaxActionRowComponents)
            result.AddViolation($"{path}: too many action row components: {actionRow.Components.Count} (max {MaxActionRowComponents}).");

        foreach (var child in actionRow.Components)
        {
            context.TotalComponentCount++;

            switch (child)
            {
                case ButtonComponent button:
                    ValidateButton(button, $"{path}/ButtonComponent", result);
                    break;
                case SelectMenuComponent menu:
                    ValidateSelectMenu(menu, $"{path}/SelectMenuComponent", result);
                    break;
            }
        }
    }

    private static void ValidateButton(ButtonComponent button, string path, ValidationResult result)
    {
        if (button.Label is { Length: > MaxButtonLabelLength })
            result.AddViolation($"{path}: button label too long: {button.Label.Length} (max {MaxButtonLabelLength}).");

        var isLink = button.Style == ButtonStyle.Link;
        if (isLink)
        {
            if (string.IsNullOrWhiteSpace(button.Url))
                result.AddViolation($"{path}: link button URL is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(button.CustomId))
        {
            result.AddViolation($"{path}: non-link button missing custom ID.");
            return;
        }

        if (button.CustomId.Length > MaxCustomIdLength)
            result.AddViolation($"{path}: button custom ID too long: {button.CustomId.Length} (max {MaxCustomIdLength}).");
    }

    private static void ValidateSelectMenu(SelectMenuComponent menu, string path, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(menu.CustomId))
        {
            result.AddViolation($"{path}: select menu missing custom ID.");
            return;
        }

        if (menu.CustomId.Length > MaxCustomIdLength)
            result.AddViolation($"{path}: select menu custom ID too long: {menu.CustomId.Length} (max {MaxCustomIdLength}).");

        if (menu.Placeholder is { Length: > MaxPlaceholderLength })
            result.AddViolation($"{path}: select menu placeholder too long: {menu.Placeholder.Length} (max {MaxPlaceholderLength}).");

        if (menu.Options is { Count: > MaxSelectMenuOptions })
            result.AddViolation($"{path}: too many select menu options: {menu.Options.Count} (max {MaxSelectMenuOptions}).");

        if (menu.MinValues < 0)
            result.AddViolation($"{path}: select menu MinValues must be >= 0.");

        if (menu.MaxValues < 1)
            result.AddViolation($"{path}: select menu MaxValues must be >= 1.");
    }

    public static void AssertValid(MessageComponent components, string context = "")
    {
#if DEBUG
        var result = Validate(components);
        if (!result.IsValid)
        {
            var contextInfo = string.IsNullOrEmpty(context) ? "" : $" [{context}]";
            System.Diagnostics.Debug.Fail($"ComponentsV2 validation failed{contextInfo}:\n{result}");
        }
#endif
    }

    public static bool TryValidate(MessageComponent components, out ValidationResult result)
    {
        result = Validate(components);
        return result.IsValid;
    }

    public static ValidationSummary GetSummary(MessageComponent components)
    {
        var totalComponents = CountAllComponents(components);
        var result = Validate(components);

        return new ValidationSummary(
            TotalComponents: totalComponents,
            IsValid: result.IsValid,
            Violations: result.Violations);
    }

    private class ValidationContext
    {
        public int TotalComponentCount { get; set; }
        public int ActionRowCount { get; set; }
        public int CumulativeTextLength { get; set; }
    }
}

public class ValidationResult
{
    private readonly List<string> _violations = new();

    public IReadOnlyList<string> Violations => _violations;
    public bool IsValid => _violations.Count == 0;

    public void AddViolation(string message) => _violations.Add(message);

    public override string ToString()
        => IsValid ? "Valid" : string.Join(Environment.NewLine, _violations);
}

public record ValidationSummary(int TotalComponents, bool IsValid, IReadOnlyList<string> Violations)
{
    public override string ToString()
        => $"Components: {TotalComponents}/40, Valid: {IsValid}" +
           (IsValid ? "" : $", Violations: {string.Join("; ", Violations)}");
}
