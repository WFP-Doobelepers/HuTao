using Discord;

namespace Zhongli.Data.Models.Discord
{
    public interface IRoleEntity : IMentionable
    {
        ulong RoleId { get; set; }

        string IMentionable.Mention => $"<@&{RoleId}>";
    }
}