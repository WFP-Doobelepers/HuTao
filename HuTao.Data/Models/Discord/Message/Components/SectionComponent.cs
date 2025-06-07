using System.Collections.Generic;

namespace HuTao.Data.Models.Discord.Message.Components;

public class SectionComponent : Component
{
    // A section might contain a list of components (e.g., TextDisplay)
    public virtual ICollection<Component> ContentComponents { get; set; } = new List<Component>();

    // Or, a section might have a more defined structure, e.g., a main content part and an accessory.
    // For example, a TextDisplayComponent as main content and a ButtonComponent as an accessory.
    // This needs to be based on how Discord structures sections.
    // For now, using a generic list of components. If a section has a fixed structure (e.g. text + optional accessory),
    // we might want specific properties like:
    // public TextDisplayComponent MainContent { get; set; }
    // public Component Accessory { get; set; } // Could be Button, SelectMenu etc.

    public Component? Accessory { get; set; } // Example: a button or select menu on the side
}