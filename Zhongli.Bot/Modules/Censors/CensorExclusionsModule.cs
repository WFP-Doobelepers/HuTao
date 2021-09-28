using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Censors
{
    [Name("Censor Exclusions")]
    [Group("censor")]
    [Alias("censors")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class CensorExclusionsModule : InteractiveEntity<Criterion>
    {
        private readonly ZhongliContext _db;

        public CensorExclusionsModule(CommandErrorHandler error, ZhongliContext db) : base(error, db) { _db = db; }

        protected override string Title => "Censor Exclusions";

        [Command("exclude")]
        [Alias("ignore")]
        [Summary("Exclude the set criteria globally in all censors.")]
        public async Task ExcludeAsync(Exclusions exclusions)
        {
            var collection = await GetCollectionAsync();
            collection.AddCriteria(exclusions);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("include")]
        [Summary("Remove a global censor exclusion by ID.")]
        protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

        [Command("exclusions")]
        [Alias("view exclusions", "list exclusions")]
        [Summary("View the configured censor exclusions.")]
        protected async Task ViewExclusionsAsync()
        {
            var collection = await GetCollectionAsync();
            await PagedViewAsync(collection);
        }

        protected override (string Title, StringBuilder Value) EntityViewer(Criterion entity)
            => (entity.Id.ToString(), new StringBuilder($"{entity}"));

        protected override bool IsMatch(Criterion entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task RemoveEntityAsync(Criterion censor)
        {
            _db.Remove(censor);
            await _db.SaveChangesAsync();
        }

        protected override async Task<ICollection<Criterion>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            return guild.ModerationRules.CensorExclusions;
        }

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
}