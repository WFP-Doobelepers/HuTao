using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Moderation;

[Group("category-permissions", "Manage moderation category permissions.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveCategoryPermissionsModule(HuTaoContext db)
    : InteractionModuleBase<SocketInteractionContext>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("add-role", "Add a role permission to a category.")]
    public async Task AddRolePermissionAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory category,
        AuthorizationScope scope,
        IRole role,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var criterion = new RoleCriterion(role);
        await AddPermissionAsync(category, scope, access, criterion, ephemeral);
    }

    [SlashCommand("add-user", "Add a user permission to a category.")]
    public async Task AddUserPermissionAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory category,
        AuthorizationScope scope,
        IUser user,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var criterion = new UserCriterion(user.Id);
        await AddPermissionAsync(category, scope, access, criterion, ephemeral);
    }

    [SlashCommand("add-channel", "Add a channel permission to a category.")]
    public async Task AddChannelPermissionAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory category,
        AuthorizationScope scope,
        IGuildChannel channel,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var criterion = new ChannelCriterion(channel.Id, channel is ICategoryChannel);
        await AddPermissionAsync(category, scope, access, criterion, ephemeral);
    }

    [SlashCommand("add-permission", "Add a server permission requirement to a category.")]
    public async Task AddGuildPermissionAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory category,
        AuthorizationScope scope,
        GuildPermission permission,
        AccessType access = AccessType.Allow,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var criterion = new PermissionCriterion(permission);
        await AddPermissionAsync(category, scope, access, criterion, ephemeral);
    }

    [SlashCommand("list", "View category permissions.")]
    public async Task ListPermissionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();

        if (collection.Count == 0)
        {
            await FollowupAsync("No category permissions configured.", ephemeral: true);
            return;
        }

        var embeds = collection
            .Take(10)
            .Select(a => BuildEmbed(a).Build())
            .ToArray();

        await FollowupAsync(embeds: embeds, ephemeral: ephemeral);
    }

    [SlashCommand("remove", "Remove a category permission by group ID.")]
    public async Task RemovePermissionAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!Guid.TryParse(id, out var guid))
        {
            await FollowupAsync("Invalid ID format.", ephemeral: true);
            return;
        }

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var group = guild.ModerationCategories
            .SelectMany(c => c.Authorization)
            .FirstOrDefault(g => g.Id == guid);

        if (group is null)
        {
            await FollowupAsync("Permission group not found.", ephemeral: true);
            return;
        }

        db.TryRemove(group);
        db.RemoveRange(group.Collection);
        await db.SaveChangesAsync();

        await FollowupAsync($"Permission group `{id}` removed.", ephemeral: ephemeral);
    }

    private async Task AddPermissionAsync(
        ModerationCategory category, AuthorizationScope scope,
        AccessType access, Criterion criterion, bool ephemeral)
    {
        var group = new AuthorizationGroup(scope, access, JudgeType.Any, criterion);
        category.Authorization.Add(group.WithModerator((IGuildUser)Context.User));
        await db.SaveChangesAsync();

        var embed = BuildEmbed(new Authorization(category, group)).WithColor(Color.Green).Build();
        var container = embed.ToComponentsV2Container(accentColor: AccentColor, maxChars: 3800);
        var components = new ComponentBuilderV2().WithContainer(container).Build();
        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    private static EmbedBuilder BuildEmbed(Authorization auth)
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
            embed.AddField(e => e
                .WithName(rules.Key.Name
                    .Replace(nameof(Criterion), string.Empty)
                    .Pluralize().Humanize(LetterCasing.Title))
                .WithValue(rules.Humanize())
                .WithIsInline(true));
        }

        return embed;
    }

    private async Task<IList<Authorization>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationCategories
            .SelectMany(category => category.Authorization
                .Select(auth => new Authorization(category, auth))
                .DefaultIfEmpty(new Authorization(category, new AuthorizationGroup())))
            .ToList();
    }
}
