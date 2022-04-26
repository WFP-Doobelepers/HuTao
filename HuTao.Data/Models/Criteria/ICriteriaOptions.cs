using System.Collections.Generic;
using Discord;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Data.Models.Criteria;

public interface ICriteriaOptions
{
    public GuildPermission Permission { get; }

    public IEnumerable<IGuildChannel>? Channels { get; }

    public IEnumerable<IGuildUser>? Users { get; }

    public IEnumerable<IRole>? Roles { get; }
}