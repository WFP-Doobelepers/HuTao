using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class CriterionExclusion : ModerationExclusion
{
    protected CriterionExclusion() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public CriterionExclusion(Criterion criterion, AutoConfiguration? config)
    {
        Criterion     = criterion;
        Configuration = config;
    }

    public virtual Criterion Criterion { get; set; } = null!;
}