using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data.Models.Discord;

namespace HuTao.Services.Utilities;

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
        => user.DiscriminatorValue == 0 ? user.Username : $"{user.Username}#{user.Discriminator}";

    public static Task<IGuildUser> GetUserAsync(this IGuildUserEntity entity, IGuild guild)
        => guild.GetUserAsync(entity.UserId);

    public static Task<IGuildUser> GetUserAsync(this IGuildUserEntity entity, Context context)
        => entity.GetUserAsync(context.Guild);

    public static Task<IUser> GetUserAsync(this IGuildUserEntity entity, IDiscordClient client)
        => client.GetUserAsync(entity.UserId);
}