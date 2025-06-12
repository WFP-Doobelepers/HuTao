using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class TimeoutAction(TimeSpan? length) : ReprimandAction, ITimeout
{
    [Column(nameof(ILength.Length))] public TimeSpan? Length { get; set; } = length;
}