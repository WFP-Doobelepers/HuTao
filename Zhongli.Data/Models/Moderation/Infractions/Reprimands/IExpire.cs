using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands
{
    public interface IExpire
    {
        bool IsActive { get; }

        DateTimeOffset? EndAt { get; }

        TimeSpan? TimeLeft { get; }

        DateTimeOffset? EndedAt { get; set; }

        DateTimeOffset? StartedAt { get; set; }

        TimeSpan? Length { get; set; }
    }
}