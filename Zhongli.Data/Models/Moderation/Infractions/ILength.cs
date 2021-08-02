using System;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface ILength
    {
        TimeSpan? Length { get; set; }
    }
}