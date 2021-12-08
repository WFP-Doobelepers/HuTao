using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria;

public class ChannelCriterion : Criterion, IGuildChannelEntity
{
    protected ChannelCriterion() { }

    public ChannelCriterion(ulong channelId, bool isCategory)
    {
        ChannelId  = channelId;
        IsCategory = isCategory;
    }

    public ulong ChannelId { get; set; }

    public bool IsCategory { get; set; }

    public override string ToString() => this.MentionChannel();
}