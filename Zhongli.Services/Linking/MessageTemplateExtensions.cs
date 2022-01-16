using Discord;
using Zhongli.Data.Models.Discord.Message.Linking;

namespace Zhongli.Services.Linking;

public static class MessageTemplateExtensions
{
    public static string GetJumpUrl(this MessageTemplate template, IGuild guild)
        => $"https://discord.com/channels/{guild.Id}/{template.ChannelId}/{template.MessageId}";
}