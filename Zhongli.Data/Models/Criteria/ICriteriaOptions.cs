using System.Collections.Generic;
using Discord;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Data.Models.Criteria;

public interface ICriteriaOptions
{
    public GuildPermission Permission { get; set; }

    public IEnumerable<IGuildChannel>? Channels { get; set; }

    public IEnumerable<IGuildUser>? Users { get; set; }

    public IEnumerable<IRole>? Roles { get; set; }
}