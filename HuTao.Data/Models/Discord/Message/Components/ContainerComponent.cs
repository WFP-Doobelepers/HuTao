using System.Collections.Generic;

namespace HuTao.Data.Models.Discord.Message.Components;

public class ContainerComponent : Component
{
    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public uint? AccentColor { get; set; }

    public bool Spoiler { get; set; }
}