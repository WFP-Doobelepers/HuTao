using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public interface IAutoConfigurationOptions : ITrigger
{
    public const string AmountSummary
        = "The threshold limit that will be used to determine if the filter is triggered. Defaults to `1`.";

    public const string CategorySummary = "The category that will be used for the reprimand action.";
    public const string CooldownSummary = "The cooldown before the filter can be triggered again.";
    public const string DeleteMessagesSummary = "Delete the messages that trigger the filter. Defaults to `True`.";

    public const string DuplicatePercentageSummary
        = "The minimum percentage of duplicates to trigger the filter. Defaults to `0`.";

    public const string DuplicateToleranceSummary
        = "The tolerance of the duplicate filter. `0` means they must be exact. Defaults to `0`.";

    public const string DuplicateTypeSummary
        = "The way how duplicate will be calculated. Defaults to `Message`.";

    public const string GlobalFilterSummary
        = "Consider messages globally, instead of per channel. Defaults to `False`.";

    public const string MentionCountDuplicatesSummary
        = "Count duplicates individually instead of as a single mention. Defaults to `False`.";

    public const string MentionCountInvalidSummary
        = "Count mentions even if invalid or do not actually ping a user. Defaults to `False`.";

    public const string MentionCountRoleMembersSummary
        = "Count the members of the role individually. Defaults to `False`.";

    public const string MinimumLengthSummary = "The minimum length of the message to trigger. Defaults to `0`.";

    public const string ModeSummary
        = "The behavior in which the reprimand of the filter triggers. Defaults to `Retroactive`.";

    public const string NewLineBlankOnlySummary
        = "NewLine Spam: Count NewLines from blank lines only. Defaults to `False`.";

    public const string ReasonSummary = "The custom reason message that will be used when the filter is triggered.";
    public const string TimePeriodSummary = "The length of time to consider the messages for the filter.";

    public bool DeleteMessages { get; set; }

    public bool GlobalFilter { get; set; }

    public bool MentionCountDuplicates { get; set; }

    public bool MentionCountInvalid { get; set; }

    public bool MentionCountRoleMembers { get; set; }

    public bool NewLineBlankOnly { get; set; }

    public double DuplicatePercentage { get; set; }

    public DuplicateConfiguration.DuplicateType DuplicateType { get; set; }

    public int DuplicateTolerance { get; set; }

    public int MinimumLength { get; set; }

    public string? Reason { get; set; }

    public TimeSpan TimePeriod { get; set; }

    public TimeSpan? Cooldown { get; set; }
}

public abstract class AutoConfiguration : Trigger, ITriggerAction
{
    protected AutoConfiguration() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected AutoConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options) : base(options)
    {
        DeleteMessages = options.DeleteMessages;
        Global         = options.GlobalFilter;
        MinimumLength  = options.MinimumLength;
        Reason         = options.Reason;
        Length         = options.TimePeriod;
        Cooldown       = options.Cooldown;
        Reprimand      = reprimand;
    }

    [Column(nameof(ReprimandId))]
    public Guid? ReprimandId { get; set; }

    public bool DeleteMessages { get; set; }

    public bool Global { get; set; }

    public virtual ICollection<ModerationExclusion> Exclusions { get; set; } = new List<ModerationExclusion>();

    public int MinimumLength { get; set; }

    public string? Reason { get; set; }

    public TimeSpan Length { get; set; }

    public TimeSpan? Cooldown { get; set; }

    public virtual ReprimandAction? Reprimand { get; set; }
}