using Discord;

namespace Zhongli.Data.Models.Discord
{
    public interface IUserEntity : IMentionable
    {
        ulong UserId { get; set; }

        string IMentionable.Mention => $"<@{UserId}>";
    }
}