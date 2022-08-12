using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Logging;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Logging;

[Name("Logging Exclusions")]
[Group("log")]
[Alias("logs", "logging")]
[Summary("Manages logging exclusions.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class LoggingExclusionsModule : InteractiveEntity<Criterion>
{
    private readonly HuTaoContext _db;

    public LoggingExclusionsModule(HuTaoContext db) { _db = db; }

    protected virtual string Title => "Censor Exclusions";

    [Command("exclude")]
    [Alias("ignore")]
    [Summary("Exclude the set criteria globally in logging.")]
    public async Task ExcludeAsync(Exclusions exclusions)
    {
        var collection = await GetCollectionAsync();
        collection.AddCriteria(exclusions);

        await _db.SaveChangesAsync();

        var embed = new EmbedBuilder()
            .WithTitle("Logging exclusions added")
            .WithColor(Color.Green)
            .AddField("Excluded: ", exclusions.ToCriteria().Humanize())
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("include")]
    [Summary("Remove a global logging exclusion by ID.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("exclusions")]
    [Alias("view exclusions", "list exclusions")]
    [Summary("View the configured logging exclusions.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(Criterion entity) => entity.ToEmbedBuilder();

    protected override string Id(Criterion entity) => entity.Id.ToString();

    protected override async Task<ICollection<Criterion>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.LoggingRules ??= new LoggingRules();
        return guild.LoggingRules.LoggingExclusions;
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

        [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
        public JudgeType JudgeType { get; set; } = JudgeType.Any;
    }
}