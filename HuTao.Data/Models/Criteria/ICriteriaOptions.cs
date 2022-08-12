using System.Collections.Generic;
using Discord;
using HuTao.Data.Models.Authorization;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Data.Models.Criteria;

public interface ICriteriaOptions
{
    public GuildPermission Permission { get; }

    public IEnumerable<IGuildChannel>? Channels { get; }

    public IEnumerable<IGuildUser>? Users { get; }

    public IEnumerable<IRole>? Roles { get; }

    public JudgeType JudgeType { get; }
}

public interface IJudge<in T>
{
    public bool Judge(T judge);
}