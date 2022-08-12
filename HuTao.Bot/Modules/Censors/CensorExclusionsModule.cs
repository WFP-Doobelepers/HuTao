using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Censors;

[Name("Censor Exclusions")]
[Group("censor")]
[Alias("censors")]
[Summary("Manages censor exclusions.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class CensorExclusionsModule : InteractiveEntity<Criterion>
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public CensorExclusionsModule(HuTaoContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    protected virtual string Title => "Censor Exclusions";

    [Command("exclude")]
    [Alias("ignore")]
    [Summary("Exclude the set criteria globally in all censors.")]
    public async Task ExcludeAsync(Exclusions exclusions)
    {
        await AddEntitiesAsync(exclusions.ToCriteria());
        _cache.InvalidateCaches(Context.Guild);
    }

    [Command("include")]
    [Summary("Remove a global censor exclusion by ID.")]
    protected override async Task RemoveEntityAsync(string id)
    {
        await base.RemoveEntityAsync(id);
        _cache.InvalidateCaches(Context.Guild);
    }

    [Command("exclusions")]
    [Alias("view exclusions", "list exclusions")]
    [Summary("View the configured censor exclusions.")]
    protected async Task ViewExclusionsAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    protected override EmbedBuilder EntityViewer(Criterion entity) => entity.ToEmbedBuilder();

    protected override string Id(Criterion entity) => entity.Id.ToString();

    protected override async Task<ICollection<Criterion>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
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

        [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
        public JudgeType JudgeType { get; set; } = JudgeType.Any;
    }
}