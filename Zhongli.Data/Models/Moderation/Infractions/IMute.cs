using System;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IMute
    {
        public TimeSpan? Length { get; set; }
    }
}