using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Reaction;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Utilities
{
    public static class DbSetExtensions
    {
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

        public static async Task<ReactionEntity> TrackEmoteAsync(this DbContext db, IEmote reaction,
            CancellationToken cancellationToken = default)
        {
            if (reaction is Emote emote)
            {
                var entity = await db.Set<EmoteEntity>().ToAsyncEnumerable()
                    .FirstOrDefaultAsync(e => e.EmoteId == emote.Id, cancellationToken);

                return entity ?? db.Add(new EmoteEntity(emote)).Entity;
            }
            else
            {
                var entity = await db.Set<EmojiEntity>().ToAsyncEnumerable()
                    .FirstOrDefaultAsync(e => e.Name == reaction.Name, cancellationToken);

                return entity ?? db.Add(new EmojiEntity(reaction)).Entity;
            }
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

        public static async ValueTask<GuildUserEntity> TrackUserAsync(
            this DbSet<GuildUserEntity> set, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var user = await details.GetUserAsync();
            if (user is not null) return await set.TrackUserAsync(user, cancellationToken);

            var userEntity =
                await set.FindAsync(new object[] { details.User.Id, details.Guild.Id }, cancellationToken)
                ?? set.Add(new GuildUserEntity(details.User, details.Guild)).Entity;

            return userEntity;
        }

        public static ValueTask<T?> FindByIdAsync<T>(this DbSet<T> dbSet, object key,
            CancellationToken cancellationToken = default)
            where T : class => dbSet.FindAsync(new[] { key }, cancellationToken)!;
    }
}