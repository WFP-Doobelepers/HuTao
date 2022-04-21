using System;
using System.ComponentModel.DataAnnotations;

namespace HuTao.Data.Models.Discord.Message.Components;

public abstract class Component
{
    public Guid Id { get; set; }

    public bool IsDisabled { get; set; }

    [MaxLength(100)] public string? CustomId { get; set; }
}