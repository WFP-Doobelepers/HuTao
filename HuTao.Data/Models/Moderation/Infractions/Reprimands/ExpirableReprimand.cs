using System;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public abstract class ExpirableReprimand : Reprimand, IExpirable
{
    protected ExpirableReprimand() { }

    protected ExpirableReprimand(TimeSpan? length, ReprimandDetails details) : base(details)
    {
        Length    = length;
        StartedAt = DateTimeOffset.UtcNow;
        ExpireAt  = StartedAt + Length;
    }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset? ExpireAt { get; set; }

    public TimeSpan? Length { get; set; }
}