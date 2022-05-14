using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Data.Models.Criteria;

namespace HuTao.Data.Models.Discord.Message.Linking;

public interface IRoleCriteriaOptions
{
    public IEnumerable<IRole>? DeniedRoles { get; }

    public IEnumerable<IRole>? RequiredRoles { get; }

    public IEnumerable<RoleCriterion> Allowed => ToCriteria(RequiredRoles);

    public IEnumerable<RoleCriterion> Denied => ToCriteria(DeniedRoles);

    private static IEnumerable<RoleCriterion> ToCriteria(IEnumerable<IRole>? roles)
        => roles?.Select(r => new RoleCriterion(r)) ?? Enumerable.Empty<RoleCriterion>();
}