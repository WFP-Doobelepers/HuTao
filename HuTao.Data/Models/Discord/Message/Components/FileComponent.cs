using System.ComponentModel.DataAnnotations;

namespace HuTao.Data.Models.Discord.Message.Components;

public class FileComponent : Component
{
    [MaxLength(256)] // Placeholder
    public string? FileName { get; set; }

    public long FileSize { get; set; } // In bytes

    [Url]
    public string? Url { get; set; } // URL to the file if hosted by Discord or externally

    [MaxLength(1024)] // Placeholder
    public string? Description { get; set; }

    // For V2, files might have previews or be displayed differently.
    // Additional properties might be needed based on API specifics.
    // e.g., ContentType, IsSpoiler, etc.

    public FileComponent()
    {
        // Type = ComponentType.File; // We don't have a 'File' in our ComponentType enum yet.
                                  // Need to decide if this is a distinct component type or part of message attachments.
                                  // For now, I will comment this out. We should revisit ComponentType.cs
    }
} 