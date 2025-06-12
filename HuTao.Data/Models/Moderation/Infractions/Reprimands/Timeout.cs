using System;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class Timeout : ExpirableReprimand
{
    protected Timeout() { }

    public Timeout(TimeSpan? length, ReprimandDetails details) : base(length, details) { }
}