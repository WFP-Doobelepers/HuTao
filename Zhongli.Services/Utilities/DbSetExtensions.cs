using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Services.Utilities
{
    public static class DbSetExtensions
    {
        public static ValueTask<T?> FindByIdAsync<T>(this DbSet<T> dbSet, object key,
            CancellationToken cancellationToken)
            where T : class => dbSet.FindAsync(new[] { key }, cancellationToken)!;

        public static async Task<GuildEntity> TrackGuildAsync(this DbSet<GuildEntity> set, IGuild guild,
            CancellationToken cancellationToken = default)
        {
            var guildEntity = await set.FindByIdAsync(guild.Id, cancellationToken)
                ?? set.Add(new GuildEntity(guild.Id)).Entity;

            return guildEntity;
        }

        public static T AsProxy<T>(this T entity, DbContext context) where T : class
        {
            var proxy = context.CreateProxy<T>();
            context.Entry(proxy).CurrentValues.SetValues(entity);

            return proxy;
        }

        public static async Task<GuildUserEntity> TrackUserAsync(this DbSet<GuildUserEntity> set, IGuildUser user,
            CancellationToken cancellationToken = default)
        {
            var userEntity = await set
                .FindAsync(new object[] { user.Id, user.Guild.Id }, cancellationToken);

            if (userEntity is null)
            {
                userEntity = set.Add(new GuildUserEntity(user)).Entity;
            }
            else
            {
                userEntity.Username           = user.Username;
                userEntity.Nickname           = user.Nickname;
                userEntity.DiscriminatorValue = user.DiscriminatorValue;
            }

            return userEntity;
        }
    }
}