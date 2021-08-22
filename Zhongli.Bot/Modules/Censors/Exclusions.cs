using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Criteria;
using Zhongli.Services.CommandHelp;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Censors
{
    [NamedArgumentType]
    public class Exclusions : ICriteriaOptions
    {
        [HelpSummary("The permissions that the user must have.")]
        public GuildPermission Permission { get; set; } = GuildPermission.None;

        [HelpSummary("The text or category channels that will be excluded.")]
        public IEnumerable<IGuildChannel>? Channels { get; set; }

        [HelpSummary("The users that are excluded.")]
        public IEnumerable<IGuildUser>? Users { get; set; }

        [HelpSummary("The roles that are excluded.")]
        public IEnumerable<IRole>? Roles { get; set; }
    }
}