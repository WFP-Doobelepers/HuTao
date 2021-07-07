using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Zhongli.Services.Utilities
{
    public static class UserExtensions
    {
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

        public static string GetFullUsername(this IUser user)
            => $"{user.Username}#{user.Discriminator}";
    }
}