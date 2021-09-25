using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.NamedArguments;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Logging
{
    [Name("Logging Exclusions")]
    [Group("log")]
    [Alias("logs", "logging")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class LoggingExclusionsModule : InteractiveEntity<Criterion>
    {
        private readonly ZhongliContext _db;

        public LoggingExclusionsModule(CommandErrorHandler error, ZhongliContext db) : base(error, db) { _db = db; }

        protected override string Title => "Censor Exclusions";

        [Command("exclude")]
        [Alias("ignore")]
        [Summary("Exclude the set criteria globally in logging.")]
        public async Task ExcludeAsync(Exclusions exclusions)
        {
            var collection = await GetCollectionAsync();
            collection.AddCriteria(exclusions);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("include")]
        [Summary("Remove a global logging exclusion by ID.")]
        protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

        [Command("exclusions")]
        [Alias("view exclusions", "list exclusions")]
        [Summary("View the configured logging exclusions.")]
        protected async Task ViewExclusionsAsync()
        {
            var collection = await GetCollectionAsync();
            await PagedViewAsync(collection);
        }

        protected override (string Title, StringBuilder Value) EntityViewer(Criterion entity)
            => (entity.Id.ToString(), new StringBuilder($"{entity}"));

        protected override bool IsMatch(Criterion entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task RemoveEntityAsync(Criterion criterion)
        {
            _db.Remove(criterion);
            await _db.SaveChangesAsync();
        }

        protected override async Task<ICollection<Criterion>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            return guild.LoggingRules.LoggingExclusions;
        }
    }
}