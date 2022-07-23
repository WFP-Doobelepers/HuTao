using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Services.CommandHelp;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.AutoModeration;

[Name("Auto Moderation Exclusions")]
[Group("auto")]
public class ModerationExclusionsModule : InteractiveEntity<ModerationExclusion>
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public ModerationExclusionsModule(HuTaoContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    [Command("exclude")]
    [Alias("ignore")]
    [Summary("Add an exclusion to the auto-moderation system.")]
    public async Task ExcludeAsync(ModerationExclusionsOptions options)
    {
        await AddEntitiesAsync(await options.GetExclusionsAsync(_db, options.Configuration));
        _cache.InvalidateCaches(Context.Guild);
    }

    [Command("include")]
    [Summary("Remove an exclusion by ID.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("exclusions")]
    [Alias("view exclusions", "list exclusions")]
    [Summary("View the configured censor exclusions.")]
    protected async Task ViewExclusionsAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    protected override EmbedBuilder EntityViewer(ModerationExclusion entity) => new EmbedBuilder()
        .WithTitle($"Exclusion: {entity.Id}")
        .WithDescription(entity.GetDetails(Context))
        .AddField("Configuration", $"{entity.Configuration?.Id}".DefaultIfNullOrEmpty("None"));

    protected override string Id(ModerationExclusion entity) => entity.Id.ToString();

    protected override async Task<ICollection<ModerationExclusion>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();

        return guild.ModerationRules.Exclusions;
    }
}

[NamedArgumentType]
public class ModerationExclusionsOptions : ICriteriaOptions
{
    [HelpSummary("The spam configuration to add the exclusion to.")]
    public AutoConfiguration? Configuration { get; set; }

    [HelpSummary("The emojis that are excluded from the emoji filter.")]
    public IEnumerable<IEmote>? Emojis { private get; set; }

    [HelpSummary("The user mentions that are excluded.")]
    public IEnumerable<IGuildUser>? UserMentions { get; set; }

    [HelpSummary("The invites that are excluded from the invite filter. You only need to give one invite per Guild.")]
    public IEnumerable<IInvite>? Invites { private get; set; }

    [HelpSummary("The role mentions that are excluded.")]
    public IEnumerable<IRole>? RoleMentions { get; set; }

    [HelpSummary("The links that are excluded from the link filter.")]
    public IEnumerable<Link>? Links { private get; set; }

    [HelpSummary("Permissions that will make the user excluded from this filter.")]
    public GuildPermission Permission { get; set; } = GuildPermission.None;

    [HelpSummary("The text or category channels that will be excluded.")]
    public IEnumerable<IGuildChannel>? Channels { get; set; }

    [HelpSummary("The users that are excluded.")]
    public IEnumerable<IGuildUser>? Users { get; set; }

    [HelpSummary("The roles that are excluded.")]
    public IEnumerable<IRole>? Roles { get; set; }

    [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
    public JudgeType JudgeType { get; set; } = JudgeType.Any;

    public async Task<ICollection<ModerationExclusion>> GetExclusionsAsync(HuTaoContext db, AutoConfiguration? config)
    {
        var criteria = this.ToCriteria();
        var links = Links ?? Enumerable.Empty<Link>();
        var emojis = await db.TrackEmotesAsync(Emojis ?? Enumerable.Empty<IEmote>()).ToListAsync();
        var invites = await db.Guilds.TrackGuildsAsync(Invites ?? Enumerable.Empty<IInvite>()).ToListAsync();
        var roles = await db.Roles.TrackRolesAsync(RoleMentions ?? Enumerable.Empty<IRole>()).ToListAsync();
        var users = await db.Users.TrackUsersAsync(UserMentions ?? Enumerable.Empty<IGuildUser>()).ToListAsync();

        return new IEnumerable<ModerationExclusion>[]
        {
            criteria.Select(c => new CriterionExclusion(c, config)),
            links.Select(l => new LinkExclusion(l, config)),
            emojis.Select(e => new EmojiExclusion(e, config)),
            invites.Select(i => new InviteExclusion(i, config)),
            roles.Select(r => new RoleMentionExclusion(r, config)),
            users.Select(u => new UserMentionExclusion(u, config))
        }.SelectMany(e => e).ToList();
    }
}