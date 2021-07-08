using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria
{
    public class GuildCriterion : Criterion, IGuildEntity
    {
        public ulong GuildId { get; set; }

        public bool JudgeAsync(ICommandContext sourceContext, IGuild parameter)
            => parameter.Id == GuildId;
    }
}