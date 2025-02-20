using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class MuteAction(TimeSpan? length) : ReprimandAction, IMute
{
    [Column(nameof(ILength.Length))] public TimeSpan? Length { get; set; } = length;
}