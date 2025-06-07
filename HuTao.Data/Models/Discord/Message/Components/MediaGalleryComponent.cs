using System.Collections.Generic;

namespace HuTao.Data.Models.Discord.Message.Components;

public class MediaGalleryComponent : Component
{
    public virtual ICollection<MediaGalleryItemComponent> Items { get; set; } = new List<MediaGalleryItemComponent>();
}