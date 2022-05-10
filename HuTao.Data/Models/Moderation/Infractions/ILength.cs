using System;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface ILength
{
    TimeSpan? Length { get; set; }
}