using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("permissions", "Manage guild authorization rules.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractivePermissionsModule(HuTaoContext db)
    : InteractionEntity<AuthorizationGroup>
{
    [SlashCommand("add-role", "Add a role-based permission rule.")]
    public async Task AddRolePermissionAsync(
        AuthorizationScope scope,
        IRole role,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var moderator = (IGuildUser)Context.User;
        var rules = new List<Criterion> { new RoleCriterion(role) };
        var group = new AuthorizationGroup(scope, access, JudgeType.Any, rules);

        var collection = await GetCollectionAsync();
        collection.Add(group.WithModerator(moderator));
        await Db.SaveChangesAsync();

        await ReplyGroupAsync(group, ephemeral);
    }

    [SlashCommand("add-user", "Add a user-based permission rule.")]
    public async Task AddUserPermissionAsync(
        AuthorizationScope scope,
        IUser user,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var moderator = (IGuildUser)Context.User;
        var rules = new List<Criterion> { new UserCriterion(user.Id) };
        var group = new AuthorizationGroup(scope, access, JudgeType.Any, rules);

        var collection = await GetCollectionAsync();
        collection.Add(group.WithModerator(moderator));
        await Db.SaveChangesAsync();

        await ReplyGroupAsync(group, ephemeral);
    }

    [SlashCommand("add-channel", "Add a channel-based permission rule.")]
    public async Task AddChannelPermissionAsync(
        AuthorizationScope scope,
        IGuildChannel channel,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var moderator = (IGuildUser)Context.User;
        var rules = new List<Criterion> { new ChannelCriterion(channel.Id, channel is ICategoryChannel) };
        var group = new AuthorizationGroup(scope, access, JudgeType.Any, rules);

        var collection = await GetCollectionAsync();
        collection.Add(group.WithModerator(moderator));
        await Db.SaveChangesAsync();

        await ReplyGroupAsync(group, ephemeral);
    }

    [SlashCommand("list", "View all configured authorization groups.")]
    public async Task ListPermissionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove an authorization group by ID.")]
    protected override async Task RemoveEntityAsync(
        [Autocomplete(typeof(PermissionAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);

        if (entity is null)
        {
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
            return;
        }

        if (entity.Action is not null) Db.Remove(entity.Action);
        Db.RemoveRange(entity.Collection);
        Db.Remove(entity);
        await Db.SaveChangesAsync();
    }

    protected override EmbedBuilder EntityViewer(AuthorizationGroup group)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{group.Scope}: {group.Id}").WithTimestamp(group)
            .WithColor(group.Access is AccessType.Allow ? Color.Green : Color.Red)
            .AddField("Type", Format.Bold(group.Access.Humanize()), true)
            .AddField("Judge", Format.Bold(group.JudgeType.Humanize()), true)
            .AddField("Scope", Format.Bold(group.Scope.Humanize()), true)
            .AddField("Moderator", group.GetModerator(), true);

        foreach (var rules in group.Collection.ToLookup(g => g.GetCriterionType()))
        {
            embed.AddField(e => e
                .WithName(rules.Key.Name.Replace(nameof(Criterion), string.Empty).Pluralize().Humanize(LetterCasing.Title))
                .WithValue(rules.Humanize())
                .WithIsInline(true));
        }

        return embed;
    }

    protected override string Id(AuthorizationGroup entity) => entity.Id.ToString();

    protected override async Task<ICollection<AuthorizationGroup>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.AuthorizationGroups;
    }

    private async Task ReplyGroupAsync(AuthorizationGroup group, bool ephemeral)
    {
        var embed = EntityViewer(group).Build();
        var container = embed.ToComponentsV2Container(accentColor: 0x9B59FF, maxChars: 3800);
        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }
}
