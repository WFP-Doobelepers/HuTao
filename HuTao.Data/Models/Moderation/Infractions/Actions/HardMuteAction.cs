using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class HardMuteAction : ReprimandAction, IHardMute
{
    public HardMuteAction(TimeSpan? length) { Length = length; }

    [Column(nameof(ILength.Length))]
    public TimeSpan? Length { get; set; }
}