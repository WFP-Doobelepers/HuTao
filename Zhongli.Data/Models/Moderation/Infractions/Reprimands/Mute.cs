using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public class Mute : ExpirableReprimand, IMute
{
    protected Mute() { }

    public Mute(TimeSpan? length, ReprimandDetails details) : base(length, details) { }
}