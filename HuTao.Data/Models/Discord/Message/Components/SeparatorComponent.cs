namespace HuTao.Data.Models.Discord.Message.Components;

public enum SeparatorSpacing
{
    Small,
    Medium,
    Large
}

public class SeparatorComponent : Component
{
    public SeparatorSpacing Spacing { get; set; } = SeparatorSpacing.Medium;

    public bool IsDividerVisible { get; set; } = true;
}