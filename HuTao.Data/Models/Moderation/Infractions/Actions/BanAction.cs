using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class BanAction(uint deleteDays, TimeSpan? length) : ReprimandAction, IBan
{
    public uint DeleteDays { get; set; } = deleteDays;

    [Column(nameof(ILength.Length))]
    public TimeSpan? Length { get; set; } = length;
}