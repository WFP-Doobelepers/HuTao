using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuTao.Data.Models.Moderation.Infractions.Actions;

public class BanAction : ReprimandAction, IBan
{
    public BanAction(uint deleteDays, TimeSpan? length)
    {
        DeleteDays = deleteDays;
        Length     = length;
    }

    public uint DeleteDays { get; set; }

    [Column(nameof(ILength.Length))]
    public TimeSpan? Length { get; set; }
}