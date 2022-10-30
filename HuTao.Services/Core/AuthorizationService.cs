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
using HuTao.Services.Utilities;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core;

public class AuthorizationService
{
    private readonly HuTaoContext _db;

    public AuthorizationService(HuTaoContext db) { _db = db; }

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
            var user = await _db.Users.TrackUserAsync(context.User, context.Guild, cancellationToken);
            category = user.DefaultCategory ?? ModerationCategory.None;
        }

        return category == ModerationCategory.All || category == ModerationCategory.None
            ? await IsAuthorizedAsync(context, scope, cancellationToken)
            : IsAuthorized(context, scope, category);
    }

    public async Task<bool> IsCategoryAuthorizedAsync(Context context, AuthorizationScope scope, object? value)
    {
        var user = await _db.Users.TrackUserAsync(context.User, context.Guild);
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
                .AllAwaitAsync(async c => await IsCategoryAuthorizedAsync(context, scope, c));
        }
    }

    public async Task<GuildEntity> AutoConfigureGuild(IGuild guild,
        CancellationToken cancellationToken = default)
    {
        var guildEntity = await GetGuildAsync(guild, cancellationToken);
        var auth = guildEntity.AuthorizationGroups;

        if (auth.Any()) return guildEntity;

        var permission = new PermissionCriterion(GuildPermission.Administrator);
        auth.AddRules(
            AuthorizationScope.All, await guild.GetCurrentUserAsync(),
            AccessType.Allow, JudgeType.Any, permission);
        await _db.SaveChangesAsync(cancellationToken);

        return guildEntity;
    }

    public ValueTask<bool> IsAuthorizedAsync(SocketCommandContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new CommandContext(context), scope, cancellationToken);

    public ValueTask<bool> IsAuthorizedAsync(SocketInteractionContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new InteractionContext(context), scope, cancellationToken);

    public async ValueTask<bool> IsAuthorizedAsync(Context context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        var rules = await AutoConfigureGuild(context.Guild, cancellationToken);
        return IsAuthorized(context, rules.AuthorizationGroups.Scoped(scope).ToList());
    }

    private static bool IsAuthorized(Context context, AuthorizationScope scope, ModerationCategory category)
        => IsAuthorized(context, category.Authorization.Scoped(scope).ToList());

    private async Task<GuildEntity> GetGuildAsync(IGuild guild, CancellationToken cancellationToken = default)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
        await _db.Users.TrackUserAsync(await guild.GetCurrentUserAsync(), cancellationToken);

        return guildEntity;
    }
}