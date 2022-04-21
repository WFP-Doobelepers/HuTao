using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Criteria;

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