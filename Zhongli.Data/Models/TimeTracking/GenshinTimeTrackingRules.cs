using System;

namespace Zhongli.Data.Models.TimeTracking;

public class GenshinTimeTrackingRules
{
    public Guid Id { get; set; }

    public virtual ChannelTimeTracking? AmericaChannel { get; set; }

    public virtual ChannelTimeTracking? AsiaChannel { get; set; }

    public virtual ChannelTimeTracking? EuropeChannel { get; set; }

    public virtual ChannelTimeTracking? SARChannel { get; set; }

    public virtual MessageTimeTracking? ServerStatus { get; set; }
}