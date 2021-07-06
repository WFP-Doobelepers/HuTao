using System;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IMute
    {
        TimeSpan? Length { get; set; }
    }
}