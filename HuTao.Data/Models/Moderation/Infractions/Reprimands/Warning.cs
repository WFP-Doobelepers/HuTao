using System;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class Warning : ExpirableReprimand, IWarning
{
    protected Warning() { }

    public Warning(uint count, TimeSpan? length, ReprimandShort details) : base(length, details) { Count = count; }

    public Warning(uint count, TimeSpan? length, ReprimandDetails details) : base(length, details) { Count = count; }

    public uint Count { get; set; }
}