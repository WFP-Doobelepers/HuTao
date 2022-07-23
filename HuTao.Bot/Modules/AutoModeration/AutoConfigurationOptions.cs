using System;
using Discord.Commands;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.CommandHelp;
using static HuTao.Data.Models.Moderation.Auto.Configurations.DuplicateConfiguration;
using static HuTao.Data.Models.Moderation.Auto.Configurations.IAutoConfigurationOptions;

namespace HuTao.Bot.Modules.AutoModeration;

[NamedArgumentType]
public class AutoConfigurationOptions : IAutoConfigurationOptions
{
    [HelpSummary(DeleteMessagesSummary)]
    public bool DeleteMessages { get; set; } = true;

    [HelpSummary(GlobalFilterSummary)]
    public bool GlobalFilter { get; set; } = false;

    [HelpSummary(MentionCountDuplicatesSummary)]
    public bool MentionCountDuplicates { get; set; }

    [HelpSummary(MentionCountInvalidSummary)]
    public bool MentionCountInvalid { get; set; }

    [HelpSummary(MentionCountRoleMembersSummary)]
    public bool MentionCountRoleMembers { get; set; } = false;

    [HelpSummary(NewLineBlankOnlySummary)]
    public bool NewLineBlankOnly { get; set; }

    [HelpSummary(DuplicatePercentageSummary)]
    public double DuplicatePercentage { get; set; }

    [HelpSummary(DuplicateTypeSummary)]
    public DuplicateType DuplicateType { get; set; } = DuplicateType.Message;

    [HelpSummary(DuplicateToleranceSummary)]
    public int DuplicateTolerance { get; set; }

    [HelpSummary(MinimumLengthSummary)]
    public int MinimumLength { get; set; }

    [HelpSummary(ReasonSummary)]
    public string? Reason { get; set; }

    [HelpSummary(TimePeriodSummary)]
    public TimeSpan TimePeriod { get; set; }

    [HelpSummary(CooldownSummary)]
    public TimeSpan? Cooldown { get; set; }

    [HelpSummary(CategorySummary)]
    public ModerationCategory? Category { get; set; }

    [HelpSummary(ModeSummary)]
    public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;

    [HelpSummary(AmountSummary)]
    public uint Amount { get; set; } = 1;
}

[NamedArgumentType]
public class MuteConfigurationOptions : IAutoConfigurationOptions
{
    [HelpSummary("The length of time to mute the user for.")]
    public TimeSpan? MuteLength { get; set; }

    [HelpSummary(DeleteMessagesSummary)]
    public bool DeleteMessages { get; set; } = true;

    [HelpSummary(GlobalFilterSummary)]
    public bool GlobalFilter { get; set; } = false;

    [HelpSummary(MentionCountDuplicatesSummary)]
    public bool MentionCountDuplicates { get; set; }

    [HelpSummary(MentionCountInvalidSummary)]
    public bool MentionCountInvalid { get; set; }

    [HelpSummary(MentionCountRoleMembersSummary)]
    public bool MentionCountRoleMembers { get; set; } = false;

    [HelpSummary(NewLineBlankOnlySummary)]
    public bool NewLineBlankOnly { get; set; }

    [HelpSummary(DuplicatePercentageSummary)]
    public double DuplicatePercentage { get; set; }

    [HelpSummary(DuplicateTypeSummary)]
    public DuplicateType DuplicateType { get; set; } = DuplicateType.Message;

    [HelpSummary(DuplicateToleranceSummary)]
    public int DuplicateTolerance { get; set; }

    [HelpSummary(MinimumLengthSummary)]
    public int MinimumLength { get; set; }

    [HelpSummary(ReasonSummary)]
    public string? Reason { get; set; }

    [HelpSummary(TimePeriodSummary)]
    public TimeSpan TimePeriod { get; set; }

    [HelpSummary(CooldownSummary)]
    public TimeSpan? Cooldown { get; set; }

    [HelpSummary(CategorySummary)]
    public ModerationCategory? Category { get; set; }

    [HelpSummary(ModeSummary)]
    public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;

    [HelpSummary(AmountSummary)]
    public uint Amount { get; set; } = 1;
}

[NamedArgumentType]
public class WarningConfigurationOptions : IAutoConfigurationOptions
{
    [HelpSummary("The amount of warnings that will be given.")]
    public uint WarnCount { get; set; }

    [HelpSummary(DeleteMessagesSummary)]
    public bool DeleteMessages { get; set; } = true;

    [HelpSummary(GlobalFilterSummary)]
    public bool GlobalFilter { get; set; } = false;

    [HelpSummary(MentionCountDuplicatesSummary)]
    public bool MentionCountDuplicates { get; set; }

    [HelpSummary(MentionCountInvalidSummary)]
    public bool MentionCountInvalid { get; set; }

    [HelpSummary(MentionCountRoleMembersSummary)]
    public bool MentionCountRoleMembers { get; set; } = false;

    [HelpSummary(NewLineBlankOnlySummary)]
    public bool NewLineBlankOnly { get; set; }

    [HelpSummary(DuplicatePercentageSummary)]
    public double DuplicatePercentage { get; set; }

    [HelpSummary(DuplicateTypeSummary)]
    public DuplicateType DuplicateType { get; set; } = DuplicateType.Message;

    [HelpSummary(DuplicateToleranceSummary)]
    public int DuplicateTolerance { get; set; }

    [HelpSummary(MinimumLengthSummary)]
    public int MinimumLength { get; set; }

    [HelpSummary(ReasonSummary)]
    public string? Reason { get; set; }

    [HelpSummary(TimePeriodSummary)]
    public TimeSpan TimePeriod { get; set; }

    [HelpSummary(CooldownSummary)]
    public TimeSpan? Cooldown { get; set; }

    [HelpSummary(CategorySummary)]
    public ModerationCategory? Category { get; set; }

    [HelpSummary(ModeSummary)]
    public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;

    [HelpSummary(AmountSummary)]
    public uint Amount { get; set; } = 1;
}

[NamedArgumentType]
public class BanConfigurationOptions : IAutoConfigurationOptions
{
    [HelpSummary("The length of time to ban the user for.")]
    public TimeSpan? BanLength { get; set; }

    [HelpSummary("The amount of days to delete messages when the user is banned. Defaults to `1`.")]
    public uint DeleteDays { get; set; } = 1;

    [HelpSummary(DeleteMessagesSummary)]
    public bool DeleteMessages { get; set; } = true;

    [HelpSummary(GlobalFilterSummary)]
    public bool GlobalFilter { get; set; } = false;

    [HelpSummary(MentionCountDuplicatesSummary)]
    public bool MentionCountDuplicates { get; set; }

    [HelpSummary(MentionCountInvalidSummary)]
    public bool MentionCountInvalid { get; set; }

    [HelpSummary(MentionCountRoleMembersSummary)]
    public bool MentionCountRoleMembers { get; set; } = false;

    [HelpSummary(NewLineBlankOnlySummary)]
    public bool NewLineBlankOnly { get; set; }

    [HelpSummary(DuplicatePercentageSummary)]
    public double DuplicatePercentage { get; set; }

    [HelpSummary(DuplicateTypeSummary)]
    public DuplicateType DuplicateType { get; set; } = DuplicateType.Message;

    [HelpSummary(DuplicateToleranceSummary)]
    public int DuplicateTolerance { get; set; }

    [HelpSummary(MinimumLengthSummary)]
    public int MinimumLength { get; set; }

    [HelpSummary(ReasonSummary)]
    public string? Reason { get; set; }

    [HelpSummary(TimePeriodSummary)]
    public TimeSpan TimePeriod { get; set; }

    [HelpSummary(CooldownSummary)]
    public TimeSpan? Cooldown { get; set; }

    [HelpSummary(CategorySummary)]
    public ModerationCategory? Category { get; set; }

    [HelpSummary(ModeSummary)]
    public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;

    [HelpSummary(AmountSummary)]
    public uint Amount { get; set; } = 1;
}