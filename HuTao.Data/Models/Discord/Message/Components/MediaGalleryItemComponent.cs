using System.ComponentModel.DataAnnotations;

namespace HuTao.Data.Models.Discord.Message.Components;

public class MediaGalleryItemComponent : Component // Or a new base if not all Component fields are needed
{
    [MaxLength(100)] // Placeholder length
    public string? Description { get; set; }

    [Url]
    public string? ImageUrl { get; set; } // For the main image/video in the gallery item

    [Url]
    public string? ThumbnailUrl { get; set; } // Specifically if there's a distinct thumbnail preview

    [Url]
    public string? ClickUrl { get; set; } // URL to open when clicked
}