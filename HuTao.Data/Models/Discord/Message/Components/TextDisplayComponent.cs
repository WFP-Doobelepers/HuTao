using System.ComponentModel.DataAnnotations;

namespace HuTao.Data.Models.Discord.Message.Components;

public class TextDisplayComponent : Component
{
    [MaxLength(2000)] // Discord message content limit, adjust if specific to text display
    public string Content { get; set; } = null!;
}