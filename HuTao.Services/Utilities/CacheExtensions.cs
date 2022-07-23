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

    public static Task<LoggingRules?> GetLoggingAsync(this DbSet<GuildEntity> set, IGuild guild,
        IMemoryCache cache, CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync($"{nameof(GetLoggingAsync)}.{guild.Id}", async entry =>
        {
            entry.SetAbsoluteExpiration(CacheExpiry);
            var rules = await set.TrackGuildAsync(guild, cancellationToken);
            return rules.LoggingRules;
        });

    public static Task<ModerationRules?> GetRulesAsync(
        this DbSet<GuildEntity> set, IGuild guild, IMemoryCache cache,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync($"{nameof(GetRulesAsync)}.{guild.Id}", async entry =>
        {
            entry.SetAbsoluteExpiration(CacheExpiry);
            var entity = await set.AsSplitQuery()
                .Include(r => r.Triggers)
                .Include(r => r.Exclusions)
                .Include(r => r.CensorExclusions)
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