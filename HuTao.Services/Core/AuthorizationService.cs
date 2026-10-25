using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core;

public class AuthorizationService(HuTaoContext db)
{
    public static bool IsAuthorized(Context context, ICollection<AuthorizationGroup> groups, bool seed = false)
    {
        if (context.User.Id == HuTaoConfig.Configuration.Owner)
            return true;

        return groups.Any()
            ? groups
                .OrderBy(r => r.Action?.Date)
                .Aggregate(false, (current, rule) =>
                {
                    var passed = rule.Judge(context);
                    return passed ? rule.Access == AccessType.Allow : current;
                })
            : seed;
    }

    public async Task<bool> IsCategoryAuthorizedAsync(
        Context context, AuthorizationScope scope, ModerationCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        if (category is null)
        {
            var user = await db.Users.TrackUserAsync(context.User, context.Guild, cancellationToken);
            category = user.DefaultCategory ?? ModerationCategory.None;
        }

        return category == ModerationCategory.All || category == ModerationCategory.None
            ? await IsAuthorizedAsync(context, scope, cancellationToken)
            : IsAuthorized(context, scope, category);
    }

    /// <summary>
    ///     Whether the user has the scope in the reprimand's category. A reprimand without a category belongs to the
    ///     guild, so it is checked against the guild's rules, not the user's default category.
    /// </summary>
    public Task<bool> IsReprimandAuthorizedAsync(
        Context context, AuthorizationScope scope, Reprimand reprimand,
        CancellationToken cancellationToken = default)
        => IsCategoryAuthorizedAsync(context, scope, reprimand.Category ?? ModerationCategory.None, cancellationToken);

    /// <summary>
    ///     The reprimands the user has the scope for, in their original order, checked as
    ///     <see cref="IsReprimandAuthorizedAsync" /> does. A guild's history is long, so each category is checked once.
    /// </summary>
    public async Task<IReadOnlyList<Reprimand>> WhereReprimandsAuthorizedAsync(
        Context context, AuthorizationScope scope, IEnumerable<Reprimand> reprimands,
        CancellationToken cancellationToken = default)
    {
        var categories = new Dictionary<ModerationCategory, bool>();
        var authorized = new List<Reprimand>();

        foreach (var reprimand in reprimands)
        {
            var category = reprimand.Category ?? ModerationCategory.None;
            if (!categories.TryGetValue(category, out var allowed))
            {
                allowed = await IsCategoryAuthorizedAsync(context, scope, category, cancellationToken);
                categories[category] = allowed;
            }

            if (allowed)
                authorized.Add(reprimand);
        }

        return authorized;
    }

    public async Task<bool> IsCategoryAuthorizedAsync(Context context, AuthorizationScope scope, object? value)
    {
        var user = await db.Users.TrackUserAsync(context.User, context.Guild);
        value ??= user.DefaultCategory ?? ModerationCategory.None;

        return await (value switch
        {
            ModerationCategory c               => CheckCategory(c),
            ICategory m                        => CheckCategory(m.Category),
            ICollection<ModerationCategory?> c => CheckCategories(c),
            _                                  => throw new ArgumentException("Unknown Category Type", nameof(value))
        });

        Task<bool> CheckCategory(params ModerationCategory?[] category) => CheckCategories(category);

        async Task<bool> CheckCategories(ICollection<ModerationCategory?> categories)
        {
            if (categories.All(c => c is null))
                return await IsCategoryAuthorizedAsync(context, scope, user.DefaultCategory ?? ModerationCategory.None);

            if (categories.Any(c => c == ModerationCategory.All))
                return await IsCategoryAuthorizedAsync(context, scope, ModerationCategory.All);

            return await categories
                .ToAsyncEnumerable()
                .AllAsync(async (c, ct) => await IsCategoryAuthorizedAsync(context, scope, c, ct));
        }
    }

    public async Task<GuildEntity> AutoConfigureGuild(
        IGuild guild,
        CancellationToken cancellationToken = default)
    {
        var guildEntity = await GetGuildAsync(guild, cancellationToken);
        var auth = guildEntity.AuthorizationGroups;

        if (auth.Any()) return guildEntity;

        var permission = new PermissionCriterion(GuildPermission.Administrator);
        auth.AddRules(
            AuthorizationScope.All, await guild.GetCurrentUserAsync(),
            AccessType.Allow, JudgeType.Any, permission);
        await db.SaveChangesAsync(cancellationToken);

        return guildEntity;
    }

    public ValueTask<bool> IsAuthorizedAsync(
        SocketCommandContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new CommandContext(context), scope, cancellationToken);

    public ValueTask<bool> IsAuthorizedAsync(
        SocketInteractionContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new InteractionContext(context), scope, cancellationToken);

    public async ValueTask<bool> IsAuthorizedAsync(
        Context context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        var rules = await AutoConfigureGuild(context.Guild, cancellationToken);
        return IsAuthorized(context, rules.AuthorizationGroups.Scoped(scope).ToList());
    }

    /// <summary>
    ///     The first of the requested scopes the member does not hold themselves, or null. Granting a scope saves a rule
    ///     with the bot's authority, so the member may only grant what they have; the guild owner and Discord
    ///     administrators may grant any. For a rule in a moderation category, a scope held in that category counts too.
    /// </summary>
    public async Task<AuthorizationScope?> FindUnheldScopeAsync(
        Context context, IGuildUser member, AuthorizationScope requested, ModerationCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        if (member.Guild?.OwnerId == member.Id || member.GuildPermissions.Administrator)
            return null;

        foreach (var scope in Enum.GetValues<AuthorizationScope>())
        {
            if (scope == AuthorizationScope.None || !requested.HasFlag(scope)) continue;
            if (await IsAuthorizedAsync(context, scope, cancellationToken)) continue;
            if (category is not null && IsAuthorized(context, scope, category)) continue;

            return scope;
        }

        return null;
    }

    /// <summary>
    ///     Why the member may not add or remove the rule, or null when they may. The bot judges a rule with its own
    ///     authority, so the member may change rules only for scopes they have, in the server or in the rule's category,
    ///     and only for members and roles that rank below them.
    /// </summary>
    public async Task<string?> RefuseRuleChangeAsync(
        Context context, IGuildUser member, AuthorizationGroup rule, ModerationCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        if (await FindUnheldScopeAsync(context, member, rule.Scope, category, cancellationToken) is { } unheld)
            return $"You can only add or remove rules for scopes you have, and you do not have the {unheld} scope.";

        foreach (var criterion in rule.Collection)
        {
            switch (criterion)
            {
                case UserCriterion user
                    when await context.Guild.GetUserAsync(user.UserId) is { } target
                    && target.Hierarchy >= member.Hierarchy:
                    return $"{target.Mention} ranks at or above you, so you cannot add or remove a rule for them.";
                case RoleCriterion role
                    when context.Guild.GetRole(role.RoleId) is { } target
                    && target.Position >= member.Hierarchy:
                    return $"The {target.Name} role ranks at or above your highest role, "
                        + "so you cannot add or remove a rule for it.";
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether the user has the scope at the guild level or in any moderation category. Commands that act on a
    ///     record only learn its category once they look it up, so they use this to refuse members and then check the
    ///     record's own category.
    /// </summary>
    public async Task<bool> IsAuthorizedInAnyAsync(
        Context context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        var guild = await AutoConfigureGuild(context.Guild, cancellationToken);
        return IsAuthorized(context, guild.AuthorizationGroups.Scoped(scope).ToList())
            || guild.ModerationCategories.Any(c => IsAuthorized(context, scope, c));
    }

    private static bool IsAuthorized(Context context, AuthorizationScope scope, ModerationCategory category)
        => IsAuthorized(context, category.Authorization.Scoped(scope).ToList());

    private async Task<GuildEntity> GetGuildAsync(IGuild guild, CancellationToken cancellationToken = default)
    {
        var guildEntity = await db.Guilds.TrackGuildAsync(guild, cancellationToken);
        await db.Users.TrackUserAsync(await guild.GetCurrentUserAsync(), cancellationToken);

        return guildEntity;
    }
}