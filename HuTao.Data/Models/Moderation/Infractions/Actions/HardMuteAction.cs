using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class HardMuteAction(TimeSpan? length) : ReprimandAction, IHardMute
{
    [Column(nameof(ILength.Length))] public TimeSpan? Length { get; set; } = length;
}