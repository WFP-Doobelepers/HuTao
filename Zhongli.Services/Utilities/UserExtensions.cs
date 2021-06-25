using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Services.Utilities
{
    public static class UserExtensions
    {
        public static string GetFullUsername(this IUser user)
            => $"{user.Username}#{user.Discriminator}";

        public static bool HasRole(this IGuildUser user, ulong roleId)
            => user.RoleIds.Contains(roleId);

        public static bool HasRole(this IGuildUser user, IRole role)
            => user.HasRole(role.Id);

        public static bool HasRole(this SocketGuildUser user, ulong roleId)
            => user.Roles.Any(r => r.Id == roleId);

        public static bool HasRole(this SocketGuildUser user, IRole role)
            => user.HasRole(role.Id);

        public static string GetDefiniteAvatarUrl(this IUser user, ushort size = 128)
            => user.GetAvatarUrl(size: size) ?? user.GetDefaultAvatarUrl();

        public static async Task<GuildUserEntity> TrackUserAsync(this DbSet<GuildUserEntity> set, IGuildUser user,
            CancellationToken cancellationToken = default)
        {
            var userEntity = await set
                .FindAsync(new object[] { user.Id, user.GuildId }, cancellationToken);

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