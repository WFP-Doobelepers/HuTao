using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;

namespace Zhongli.Services.Utilities
{
    public static class DbSetExtensions
    {
        public static ValueTask<T?> FindByIdAsync<T>(this DbSet<T> dbSet, object key,
            CancellationToken cancellationToken = default)
            where T : class => dbSet.FindAsync(new[] { key }, cancellationToken)!;

        public static async Task<GuildEntity> TrackGuildAsync(this DbSet<GuildEntity> set, IGuild guild,
            CancellationToken cancellationToken = default)
        {
            var guildEntity = await set.FindByIdAsync(guild.Id, cancellationToken)
                ?? set.Add(new GuildEntity(guild.Id)).Entity;

            // ReSharper disable ConstantNullCoalescingCondition
            guildEntity.ModerationRules ??= new ModerationRules();
            guildEntity.LoggingRules    ??= new LoggingRules();
            // ReSharper restore ConstantNullCoalescingCondition

            return guildEntity;
        }

        public static async ValueTask<GuildUserEntity> TrackUserAsync(this DbSet<GuildUserEntity> set, IGuildUser user,
            CancellationToken cancellationToken = default)
        {
            var userEntity = await set
                .FindAsync(new object[] { user.Id, user.Guild.Id }, cancellationToken);

            if (userEntity is null)
                userEntity = set.Add(new GuildUserEntity(user)).Entity;
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