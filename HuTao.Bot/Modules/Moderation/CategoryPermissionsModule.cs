using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Moderation;

public record Authorization(ModerationCategory Category, AuthorizationGroup Group);

[Group("category permissions")]
[Name("Category Permissions")]
[Alias("catperms", "catperm")]
[Summary("Manages category permissions.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class CategoryPermissionsModule : InteractiveEntity<Authorization>
{
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;

    public CategoryPermissionsModule(CommandErrorHandler error, HuTaoContext db)
    {
        _error = error;
        _db    = db;
    }

    [Command("add")]
    [Summary(
        "Add a permission for a specific scope. At least one rule option must be filled. " +
        "Filling multiple options make an Authorization Group. " +
        "An Authorization Group must have all pass before the permission is allowed.")]
    public async Task AddPermissionAsync(ModerationCategory category, AuthorizationScope scope, RuleOptions options)
    {
        var rules = options.ToCriteria();
        if (rules.Count == 0)
        {
            await _error.AssociateError(Context.Message, "You must provide at least one restricting permission.");
            return;
        }

        var moderator = (IGuildUser) Context.User;
        var group = new AuthorizationGroup(scope, options.AccessType, options.JudgeType, rules);
        category.Authorization.Add(group.WithModerator(moderator));

        await _db.SaveChangesAsync();
        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    [Command]
    [Alias("view", "list")]
    [Summary("View the configured authorization groups.")]
    public async Task ViewPermissionsAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    [Command("remove")]
    [Alias("delete", "del")]
    [Summary("Remove an authorization group.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    protected override EmbedBuilder EntityViewer(Authorization entity) => GetModerationCategoryDetails(entity);

    protected override string Id(Authorization entity) => entity.Group.Id.ToString();

    protected override async Task RemoveEntityAsync(Authorization authorization)
    {
        _db.TryRemove(authorization.Group);
        _db.RemoveRange(authorization.Group.Collection);
        await _db.SaveChangesAsync();
    }

    protected override async Task<ICollection<Authorization>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationCategories
            .SelectMany(category => category.Authorization
                .Select(auth => new Authorization(category, auth))
                .DefaultIfEmpty(new Authorization(category, new AuthorizationGroup())))
            .ToList();
    }

    private static EmbedBuilder GetModerationCategoryDetails(Authorization auth)
    {
        var group = auth.Group;
        var embed = new EmbedBuilder()
            .WithTitle($"{group.Scope}: {group.Id}").WithTimestamp(group)
            .WithColor(group.Access is AccessType.Allow ? Color.Green : Color.Red)
            .AddField("Type", Format.Bold(group.Access.Humanize()), true)
            .AddField("Judge", Format.Bold(group.JudgeType.Humanize()), true)
            .AddField("Scope", Format.Bold(group.Scope.Humanize()), true)
            .AddField("Category", Format.Bold(auth.Category.Name), true)
            .AddField("Moderator", group.GetModerator(), true);

        foreach (var rules in group.Collection.ToLookup(g => g.GetCriterionType()))
        {
            string GetGroupingName() => rules.Key.Name
                .Replace(nameof(Criterion), string.Empty)
                .Pluralize().Humanize(LetterCasing.Title);

            embed.AddField(e => e
                .WithName(GetGroupingName())
                .WithValue(rules.Humanize())
                .WithIsInline(true));
        }

        return embed;
    }

    [NamedArgumentType]
    public class RuleOptions : ICriteriaOptions
    {
        [HelpSummary("Set `allow` or `deny` the matched criteria. Defaults to `allow`.")]
        public AccessType AccessType { get; set; } = AccessType.Allow;

        [HelpSummary("The permissions that the user must have.")]
        public GuildPermission Permission { get; set; }

        [HelpSummary("The text or category channels this permission will work on.")]
        public IEnumerable<IGuildChannel>? Channels { get; set; }

        [HelpSummary("The users that are allowed to use the command.")]
        public IEnumerable<IGuildUser>? Users { get; set; }

        [HelpSummary("The roles that the user must have.")]
        public IEnumerable<IRole>? Roles { get; set; }

        [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
        public JudgeType JudgeType { get; set; } = JudgeType.Any;
    }
}