using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Quote;

public static class EmbedBuilderExtensions
{
    public static EmbedBuilder AddActivity(this EmbedBuilder embed, IMessage message)
    {
        if (message.Activity is null) return embed;

        return embed
            .AddField("Invite Type", message.Activity.Type)
            .AddField("Party Id", message.Activity.PartyId);
    }

    public static EmbedBuilder AddJumpLink(this EmbedBuilder embed, IMessage message, IMentionable quotingUser)
        => embed
            .AddField("Quoted by", quotingUser.Mention, true)
            .AddField("Author", $"{message.Author.Mention} from {Format.Bold(message.GetJumpUrlForEmbed())}", true);

    public static EmbedBuilder AddJumpLink(this EmbedBuilder embed, MessageLog message, IMentionable quotingUser)
        => embed
            .AddField("Quoted by", quotingUser.Mention, true)
            .AddField("Author", $"{message.MentionUser()} from {Format.Bold(message.GetJumpUrlForEmbed())}", true);

    internal static ComponentBuilder WithQuotedMessage(this ComponentBuilder builder, QuotedMessage? quote)
    {
        if (quote?.Context.User is IGuildUser user
            && quote.Context.Guild is SocketGuild guild
            && guild.GetTextChannel(quote.ChannelId) is IGuildChannel channel
            && user.GetPermissions(channel).ManageMessages)
        {
            return builder
                .WithButton("Delete Message", $"delete:{quote.ChannelId}:{quote.MessageId}", ButtonStyle.Danger)
                .WithButton("View User", $"user:{quote.UserId}", ButtonStyle.Secondary)
                .WithButton("View Reprimands", $"history:{quote.UserId}", ButtonStyle.Secondary);
        }

        return builder;
    }

    internal static async Task<EmbedBuilder> WithMessageReference(this EmbedBuilder embed, IMessage message)
    {
        if (message.Reference is null) return embed;
        var reply = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);

        return reply is null
            ? embed.AddField("Referenced Message", message.ReferencedJumpMarkdown(), true)
            : embed.AddField($"Reply to {reply.Author}", new StringBuilder()
                .AppendLine($"{message.ReferencedJumpMarkdown()} by {reply.Author.Mention}")
                .AppendLine(reply.Content.Truncate(512)));
    }

    private static string CombinedReference(this MessageReference reference)
        => $"{reference.GuildId}/{reference.ChannelId}/{reference.MessageId}";

    private static string ReferencedJumpMarkdown(this IMessage message)
        => $"[{message.Id}]({message.ReferencedJumpUrl()}) in {MentionUtils.MentionChannel(message.Channel.Id)}";

    private static string ReferencedJumpUrl(this MessageReference reference)
        => $"https://discordapp.com/channels/{reference.CombinedReference()}";

    private static string ReferencedJumpUrl(this IMessage message)
        => message.Reference.ReferencedJumpUrl();
}