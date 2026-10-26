using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Utilities;

public static class CacheExtensions
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(1);

    public static Task<IInvite?> GetInviteAsync(this IMemoryCache cache, IDiscordClient client, string code)
        => GetInviteAsync<IInvite>(cache, client, code);

    public static Task<IInvite?> ParseInviteAsync(this IMemoryCache cache, IDiscordClient client, string input)
        => ParseInviteAsync<IInvite>(cache, client, input);

    /// <remarks>
    ///     The rules outlive the scope that loaded them, and the next messages read them from other scopes at the same
    ///     time. Their channels and exclusions are therefore loaded now: a lazy load later would run on the first scope's
    ///     database context, which is disposed by then or busy with another request.
    /// </remarks>
    public static Task<LoggingRules?> GetLoggingAsync(
        this DbSet<GuildEntity> set, IGuild guild,
        IMemoryCache cache, CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync($"{nameof(GetLoggingAsync)}.{guild.Id}", async entry =>
        {
            entry.SetAbsoluteExpiration(CacheExpiry);
            var entity = await set.AsSplitQuery()
                .Include(g => g.LoggingRules).ThenInclude(r => r!.LoggingChannels)
                .Include(g => g.LoggingRules).ThenInclude(r => r!.LoggingExclusions)
                .FirstOrDefaultAsync(g => g.Id == guild.Id, cancellationToken);
            return entity?.LoggingRules;
        });

    public static Task<ModerationRules?> GetRulesAsync(
        this DbSet<GuildEntity> set, IGuild guild, IMemoryCache cache,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync($"{nameof(GetRulesAsync)}.{guild.Id}", async entry =>
        {
            entry.SetAbsoluteExpiration(CacheExpiry);
            var entity = await set.AsSplitQuery()
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Exclusions)
                    .ThenInclude(e => ((CriterionExclusion)e).Criterion)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.CensorExclusions)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => t.Category).ThenInclude(c => c!.CensorExclusions)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => ((Censor)t).Exclusions)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => ((AutoConfiguration)t).Exclusions)
                    .ThenInclude(e => ((CriterionExclusion)e).Criterion)
                // A filter that fires reads the reprimand it gives, and a role reprimand reads its roles.
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => ((AutoConfiguration)t).Reprimand).ThenInclude(a => ((RoleAction)a!).Roles)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => ((Censor)t).Reprimand).ThenInclude(a => ((RoleAction)a!).Roles)
                .Include(g => g.ModerationRules).ThenInclude(r => r!.Triggers)
                    .ThenInclude(t => ((ReprimandTrigger)t).Reprimand).ThenInclude(a => ((RoleAction)a!).Roles)
                .FirstOrDefaultAsync(r => r.Id == guild.Id, cancellationToken);
            return entity?.ModerationRules;
        });

    public static async Task<T?> ParseInviteAsync<T>(
        this IMemoryCache cache, IDiscordClient client,
        string invite) where T : class, IInvite
    {
        var match = RegexUtilities.Invite.Match(invite);
        if (!match.Success) return null;

        var code = match.Groups["code"].Value;
        return await GetInviteAsync<T>(cache, client, code);
    }

    public static void InvalidateCaches(this IMemoryCache cache, IGuild guild)
    {
        cache.Remove($"{nameof(GetRulesAsync)}.{guild.Id}");
        cache.Remove($"{nameof(GetLoggingAsync)}.{guild.Id}");
    }

    private static IIncludableQueryable<GuildEntity, TProperty> Include<TProperty>(
        this IQueryable<GuildEntity> source, Expression<Func<ModerationRules, TProperty>> navigation)
        => source.Include(g => g.ModerationRules).ThenInclude(navigation!);

    private static async Task<T?> GetInviteAsync<T>(
        IMemoryCache cache, IDiscordClient client, string code) where T : class, IInvite
        => await cache.GetOrCreateAsync($"{nameof(ParseInviteAsync)}.{code}", async entry =>
        {
            var metadata = await client.GetInviteAsync(code) as T;
            if (metadata is RestInviteMetadata { MaxAge: > 0 } rest)
                entry.SetAbsoluteExpiration(rest.MaxAge.Value.Seconds());

            return metadata;
        });
}