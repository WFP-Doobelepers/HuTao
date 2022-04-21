using System.Collections.Generic;
using Discord;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Data.Models.Criteria;

public interface ICriteriaOptions
{
    public GuildPermission Permission { get; set; }

    public IEnumerable<IGuildChannel>? Channels { get; set; }

    public IEnumerable<IGuildUser>? Users { get; set; }

    public IEnumerable<IRole>? Roles { get; set; }
}