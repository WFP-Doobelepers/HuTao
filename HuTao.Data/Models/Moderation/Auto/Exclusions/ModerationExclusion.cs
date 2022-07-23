using System;
using System.Collections.Generic;
using System.Linq;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public abstract class ModerationExclusion
{
    protected ModerationExclusion(AutoConfiguration? config = null) { Configuration = config; }

    public Guid Id { get; set; }

    public AutoConfiguration? Configuration { get; set; }
}

public static class ModerationExclusionExtensions
{
    public static IEnumerable<EmojiExclusion> EmojiExclusions(
        this ModerationRules? rules, AutoConfiguration? configuration = null)
        => rules.Exclusions<EmojiExclusion>(configuration);

    public static IEnumerable<InviteExclusion> InviteExclusions(
        this ModerationRules? rules, AutoConfiguration? configuration = null)
        => rules.Exclusions<InviteExclusion>(configuration);

    public static IEnumerable<LinkExclusion> LinkExclusions(
        this ModerationRules? rules, AutoConfiguration? configuration = null)
        => rules.Exclusions<LinkExclusion>(configuration);

    public static IEnumerable<RoleMentionExclusion> RoleExclusions(
        this ModerationRules? rules, AutoConfiguration? configuration = null)
        => rules.Exclusions<RoleMentionExclusion>(configuration);

    public static IEnumerable<UserMentionExclusion> UserExclusions(
        this ModerationRules? rules, AutoConfiguration? configuration = null)
        => rules.Exclusions<UserMentionExclusion>(configuration);

    private static IEnumerable<ModerationExclusion> Exclusions(IEnumerable<ModerationExclusion>? exclusions)
        => exclusions ?? Enumerable.Empty<ModerationExclusion>();

    private static IEnumerable<T> Exclusions<T>(
        this ModerationRules? rules, AutoConfiguration? configuration) where T : ModerationExclusion
        => Exclusions(rules?.Exclusions).Concat(Exclusions(configuration?.Exclusions)).OfType<T>();
}