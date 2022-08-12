using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class MentionConfiguration : AutoConfiguration
{
    protected MentionConfiguration() { }

    public MentionConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options)
    {
        CountDuplicate   = options.MentionCountDuplicates;
        CountInvalid     = options.MentionCountInvalid;
        CountRoleMembers = options.MentionCountRoleMembers;
    }

    public bool CountDuplicate { get; set; }

    public bool CountInvalid { get; set; }

    public bool CountRoleMembers { get; set; }
}