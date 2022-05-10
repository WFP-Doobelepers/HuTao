using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Services.Logging;
using HuTao.Services.Utilities;

namespace HuTao.Services.Quote;

public record JumpMessage(ulong GuildId, ulong ChannelId, ulong MessageId, bool Suppressed);

public record QuotedMessage(Context Context, ulong ChannelId, ulong MessageId, ulong UserId)
    : JumpMessage(Context.Guild.Id, ChannelId, MessageId, false);

public interface IQuoteService
{
    Task<Paginator> GetPaginatorAsync(Context context,
        SocketMessage source,
        IEnumerable<JumpMessage> jumpUrls);
}

public class QuoteService : IQuoteService
{
    private const EmbedBuilderOptions QuoteOptions =
        EmbedBuilderOptions.EnlargeThumbnails |
        EmbedBuilderOptions.ReplaceAnimations;

    private readonly DiscordSocketClient _client;
    private readonly HuTaoContext _db;
    private readonly LoggingService _logging;

    public QuoteService(DiscordSocketClient client, LoggingService logging, HuTaoContext db)
    {
        _client  = client;
        _logging = logging;
        _db      = db;
    }

    public async Task<Paginator> GetPaginatorAsync(Context context, SocketMessage source,
        IEnumerable<JumpMessage> jumpUrls)
    {
        var mention = source.MentionedUsers.Any() ? AllowedMentions.All : AllowedMentions.None;
        var builder = new QuotePaginatorBuilder()
            .WithDefaultEmotes()
            .WithInputType(InputType.Buttons)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithActionOnCancellation(ActionOnStop.DeleteMessage);

        var jumpMessages = jumpUrls
            .Where(jump => !jump.Suppressed)
            .DistinctBy(j => j.MessageId);

        foreach (var jump in jumpMessages)
        {
            var message = await jump.GetMessageAsync(context);
            if (message is not null)
            {
                builder.AddPage(new QuotedPage(
                    new QuotedMessage(context, message.Channel.Id, message.Id, message.Author.Id),
                    new MultiEmbedPageBuilder()
                        .WithAllowedMentions(mention)
                        .WithMessageReference(source.Reference)
                        .WithBuilders(await BuildEmbedAsync(message, context.User))));
            }
            else
            {
                if (context.User is not IGuildUser guildUser) continue;

                var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
                var logging = guild.LoggingRules?.LoggingChannels.FirstOrDefault(l => l.Type is LogType.MessageDeleted);
                if (logging is null) continue;

                var channel = await context.Guild.GetTextChannelAsync(logging.ChannelId);
                var permissions = guildUser.GetPermissions(channel);
                if (!permissions.ViewChannel) continue;

                var log = await _logging.GetLatestMessage(jump.MessageId);
                if (log is null || log.Guild.Id != context.Guild.Id) continue;

                builder.AddPage(new QuotedPage(
                    new QuotedMessage(context, log.ChannelId, log.MessageId, log.User.Id),
                    new MultiEmbedPageBuilder()
                        .WithAllowedMentions(mention)
                        .WithMessageReference(source.Reference)
                        .WithBuilders(await BuildEmbedAsync(log, context.User))));
            }
        }

        return builder.Build();
    }

    private static async Task<IEnumerable<EmbedBuilder>> BuildEmbedAsync(IMessage message, IMentionable executingUser)
    {
        var embed = new EmbedBuilder()
            .WithColor(Color.Green)
            .AddContent(message)
            .AddActivity(message)
            .WithUserAsAuthor(message.Author, AuthorOptions.IncludeId)
            .WithTimestamp(message.Timestamp)
            .AddJumpLink(message, executingUser);

        await embed.WithMessageReference(message);
        return new[] { embed }.Concat(message.ToEmbedBuilders(QuoteOptions));
    }

    private async Task<IEnumerable<EmbedBuilder>> BuildEmbedAsync(MessageLog message, IMentionable executingUser)
    {
        var embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .AddContent(message.Content)
            .WithUserAsAuthor(await message.GetUserAsync(_client), AuthorOptions.IncludeId)
            .WithTimestamp(message.Timestamp)
            .AddJumpLink(message, executingUser);

        if (message.ReferencedMessageId is not null)
        {
            var reply = await _logging.GetLatestMessage(message.ReferencedMessageId.Value);
            var replyUser = await _logging.GetUserAsync(reply);

            embed.WithMessageReference(message, reply, replyUser);
        }

        return new[] { embed }.Concat(message.ToEmbedBuilders(QuoteOptions | EmbedBuilderOptions.UseProxy));
    }
}