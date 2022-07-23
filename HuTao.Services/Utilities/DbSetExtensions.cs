using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Services.Utilities;

public static class DbSetExtensions
{
    private static readonly SemaphoreSlim GuildSemaphore = new(1, 1);
    private static readonly SemaphoreSlim UserSemaphore = new(1, 1);

    public static IAsyncEnumerable<GuildEntity> TrackGuildsAsync(
        this DbSet<GuildEntity> set, IEnumerable<IGuild> guilds, CancellationToken cancellationToken = default)
        => guilds.ToAsyncEnumerable().SelectAwait(async g => await set.TrackGuildAsync(g.Id, cancellationToken));

    public static IAsyncEnumerable<GuildEntity> TrackGuildsAsync(
        this DbSet<GuildEntity> set, IEnumerable<IInvite> invites,
        CancellationToken cancellationToken = default) => invites
        .Where(i => i.GuildId is not null).ToAsyncEnumerable()
        .SelectAwait(async i => await set.TrackGuildAsync(i.GuildId!.Value, cancellationToken));

    public static IAsyncEnumerable<GuildUserEntity> TrackUsersAsync(
        this DbSet<GuildUserEntity> set, IEnumerable<IGuildUser> users, CancellationToken cancellationToken = default)
        => users.ToAsyncEnumerable().SelectAwait(async u => await set.TrackUserAsync(u, cancellationToken));

    public static IAsyncEnumerable<ReactionEntity> TrackEmotesAsync(
        this DbContext db, IEnumerable<IEmote> reactions, CancellationToken cancellationToken = default)
        => reactions.ToAsyncEnumerable().SelectAwait(async e => await db.TrackEmoteAsync(e, cancellationToken));

    public static IAsyncEnumerable<RoleEntity> TrackRolesAsync(
        this DbSet<RoleEntity> set, IEnumerable<IRole> roles, CancellationToken cancellationToken = default)
        => roles.ToAsyncEnumerable().SelectAwait(async g => await set.TrackRoleAsync(g, cancellationToken));

    public static async Task<ReactionEntity> TrackEmoteAsync(
        this DbContext db, IEmote reaction, CancellationToken cancellationToken = default)
    {
        if (reaction is Emote emote)
        {
            var entity = await db.Set<EmoteEntity>()
                .FirstOrDefaultAsync(e => e.EmoteId == emote.Id, cancellationToken);

            return entity ?? db.Add(new EmoteEntity(emote)).Entity;
        }
        else
        {
            var entity = await db.Set<EmojiEntity>()
                .FirstOrDefaultAsync(e => e.Name == reaction.Name, cancellationToken);

            return entity ?? db.Add(new EmojiEntity(reaction)).Entity;
        }
    }

    public static ValueTask<GuildEntity> TrackGuildAsync(
        this DbSet<GuildEntity> set, IGuild guild, CancellationToken cancellationToken = default)
        => set.TrackGuildAsync(guild.Id, cancellationToken);

    public static ValueTask<GuildUserEntity> TrackUserAsync(
        this DbSet<GuildUserEntity> set, IUser user, IGuild guild,
        CancellationToken cancellationToken = default)
        => set.TrackUserAsync(user.Id, guild.Id, cancellationToken);

    public static ValueTask<GuildUserEntity> TrackUserAsync(
        this DbSet<GuildUserEntity> set, IGuildUser user,
        CancellationToken cancellationToken = default)
        => set.TrackUserAsync(user, user.Guild, cancellationToken);

    public static ValueTask<GuildUserEntity> TrackUserAsync(
        this DbSet<GuildUserEntity> set, ReprimandDetails details,
        CancellationToken cancellationToken = default)
        => set.TrackUserAsync(details.User, details.Guild, cancellationToken);

    public static async ValueTask<RoleEntity> TrackRoleAsync(
        this DbSet<RoleEntity> set, IRole role,
        CancellationToken cancellationToken = default)
        => await set.FindByIdAsync(role.Id, cancellationToken) ?? set.Add(new RoleEntity(role)).Entity;

    public static async ValueTask<RoleEntity> TrackRoleAsync(
        this DbSet<RoleEntity> set, ulong guildId, ulong roleId,
        CancellationToken cancellationToken = default)
        => await set.FindByIdAsync(roleId, cancellationToken) ?? set.Add(new RoleEntity(guildId, roleId)).Entity;

    public static ValueTask<T?> FindByIdAsync<T>(this DbSet<T> dbSet, object key,
        CancellationToken cancellationToken = default)
        where T : class => dbSet.FindAsync(new[] { key }, cancellationToken);

    public static void TryRemove<T>(this DbContext context, T? entity)
    {
        if (entity is not null)
            context.Remove(entity);
    }

    private static async ValueTask<GuildEntity> TrackGuildAsync(
        this DbSet<GuildEntity> set, ulong guild,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await GuildSemaphore.WaitAsync(cancellationToken);

            var guildEntity = await set.FindByIdAsync(guild, cancellationToken);
            return guildEntity ?? set.Add(new GuildEntity(guild)).Entity;
        }
        finally
        {
            GuildSemaphore.Release();
        }
    }

    private static async ValueTask<GuildUserEntity> TrackUserAsync(
        this DbSet<GuildUserEntity> set, ulong user, ulong guild,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await UserSemaphore.WaitAsync(cancellationToken);

            var userEntity = await set.FindAsync(new object[] { user, guild }, cancellationToken);
            return userEntity ?? set.Add(new GuildUserEntity(user, guild)).Entity;
        }
        finally
        {
            UserSemaphore.Release();
        }
    }
}