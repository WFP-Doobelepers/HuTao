using System.Collections.Generic;
using Discord;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Data.Models.Criteria
{
    public interface ICriteriaOptions
    {
        GuildPermission Permission { get; set; }

        IEnumerable<IGuildChannel>? Channels { get; set; }

        IEnumerable<IGuildUser>? Users { get; set; }

        IEnumerable<IRole>? Roles { get; set; }
    }
}