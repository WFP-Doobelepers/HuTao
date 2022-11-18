using System;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class Mute : ExpirableReprimand, IMute
{
    protected Mute() { }

    public Mute(TimeSpan? length, ReprimandDetails details) : base(length, details) { }

    public Mute(TimeSpan? length, ReprimandShort details) : base(length, details) { }
}