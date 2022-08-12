using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class MuteAction : ReprimandAction, IMute
{
    public MuteAction(TimeSpan? length) { Length = length; }

    [Column(nameof(ILength.Length))]
    public TimeSpan? Length { get; set; }
}