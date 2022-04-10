using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Logging;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Quote;

public record JumpMessage(ulong GuildId, ulong ChannelId, ulong MessageId, bool Suppressed);

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
    private readonly LoggingService _logging;
    private readonly ZhongliContext _db;

    public QuoteService(DiscordSocketClient client, LoggingService logging, ZhongliContext db)
    {
        _client  = client;
        _logging = logging;
        _db      = db;
    }

    public async Task<Paginator> GetPaginatorAsync(Context context, SocketMessage source,
        IEnumerable<JumpMessage> jumpUrls)
    {
        var mention = source.MentionedUsers.Any() ? AllowedMentions.All : AllowedMentions.None;
        var builder = new StaticPaginatorBuilder()
            .WithInputType(InputType.Buttons)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithActionOnCancellation(ActionOnStop.DeleteMessage);

        foreach (var jump in jumpUrls.Where(jump => !jump.Suppressed))
        {
            var message = await jump.GetMessageAsync(context);
            if (message is not null)
            {
                builder.AddPage(new MultiEmbedPageBuilder()
                    .WithAllowedMentions(mention)
                    .WithMessageReference(source.Reference)
                    .WithBuilders(BuildQuoteEmbeds(message, context.User)));
            }
            else
            {
                if (context.User is not IGuildUser guildUser) continue;

                var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
                var logging = guild.LoggingRules.LoggingChannels.FirstOrDefault(l => l.Type is LogType.MessageDeleted);
                if (logging is null) continue;

                var channel = await context.Guild.GetTextChannelAsync(logging.ChannelId);
                var permissions = guildUser.GetPermissions(channel);
                if (!permissions.ViewChannel) continue;

                var log = await _logging.GetLatestMessage(jump.MessageId);
                if (log is null || log.Guild.Id != context.Guild.Id) continue;

                builder.AddPage(new MultiEmbedPageBuilder()
                    .WithAllowedMentions(mention)
                    .WithMessageReference(source.Reference)
                    .WithBuilders(await BuildQuoteEmbeds(log, context.User)));
            }
        }

        return builder.Build();
    }

    private static IEnumerable<EmbedBuilder> BuildQuoteEmbeds(IMessage message, IMentionable executingUser)
        => new List<EmbedBuilder>
        {
            new EmbedBuilder()
                .WithColor(new Color(95, 186, 125))
                .AddContent(message)
                .AddActivity(message)
                .WithUserAsAuthor(message.Author, AuthorOptions.IncludeId)
                .WithTimestamp(message.Timestamp)
                .AddJumpLink(message, executingUser)
        }.Concat(message.ToEmbedBuilders(QuoteOptions));

    private async Task<IEnumerable<EmbedBuilder>> BuildQuoteEmbeds(MessageLog message, IMentionable executingUser)
        => new List<EmbedBuilder>
        {
            new EmbedBuilder()
                .WithColor(new Color(95, 186, 125))
                .AddContent(message.Content)
                .WithUserAsAuthor(await message.GetUserAsync(_client), AuthorOptions.IncludeId)
                .WithTimestamp(message.Timestamp)
                .AddJumpLink(message, executingUser)
        }.Concat(message.ToEmbedBuilders(QuoteOptions | EmbedBuilderOptions.UseProxy));
}