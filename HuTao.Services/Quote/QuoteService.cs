using System;
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
    Task<IComponentPaginator?> GetPaginatorAsync(
        Context context,
        SocketMessage source,
        IEnumerable<JumpMessage> jumpUrls);
}

public class QuoteService(DiscordSocketClient client, LoggingService logging, HuTaoContext db)
    : IQuoteService
{
    private const EmbedBuilderOptions QuoteOptions =
        EmbedBuilderOptions.EnlargeThumbnails |
        EmbedBuilderOptions.ReplaceAnimations;

    public async Task<IComponentPaginator?> GetPaginatorAsync(
        Context context, SocketMessage source,
        IEnumerable<JumpMessage> jumpUrls)
    {
        var jumpMessages = jumpUrls
            .Where(jump => !jump.Suppressed)
            .DistinctBy(j => j.MessageId);

        var entries = new List<QuoteEntry>();

        foreach (var jump in jumpMessages)
        {
            var message = await jump.GetMessageAsync(context);
            if (message is not null)
            {
                var quote = new QuotedMessage(context, message.Channel.Id, message.Id, message.Author.Id);
                var embeds = (await BuildEmbedAsync(message, context.User)).Select(e => e.Build()).ToList();

                var canModerate = context.User is IGuildUser guildUser
                                  && message.Channel is IGuildChannel guildChannel
                                  && guildUser.GetPermissions(guildChannel).ManageMessages;

                entries.Add(QuoteEntry.FromMessage(quote, message, embeds, canModerate));
            }
            else
            {
                if (context.User is not IGuildUser guildUser) continue;

                var guild = await db.Guilds.TrackGuildAsync(context.Guild);
                var logging1
                    = guild.LoggingRules?.LoggingChannels.FirstOrDefault(l => l.Type is LogType.MessageDeleted);
                if (logging1 is null) continue;

                var channel = await context.Guild.GetTextChannelAsync(logging1.ChannelId);
                var permissions = guildUser.GetPermissions(channel);
                if (!permissions.ViewChannel) continue;

                var log = await logging.GetLatestMessage(jump.GuildId, jump.ChannelId, jump.MessageId);
                if (log is null || log.Guild.Id != context.Guild.Id) continue;

                var quote = new QuotedMessage(context, log.ChannelId, log.MessageId, log.User.Id);
                var embeds = (await BuildEmbedAsync(log, context.User)).Select(e => e.Build()).ToList();

                var quotedChannel = await context.Guild.GetTextChannelAsync(log.ChannelId);
                var canModerate = quotedChannel is not null && guildUser.GetPermissions(quotedChannel).ManageMessages;

                entries.Add(QuoteEntry.FromLog(quote, log, embeds, canModerate));
            }
        }

        if (entries.Count == 0)
            return null;

        var state = new QuotePaginatorState(entries, context.User, source.GetJumpUrl());

        var paginator = new ComponentPaginatorBuilder()
            .WithUsers(context.User)
            .WithPageCount(entries.Count)
            .WithUserState(state)
            .WithPageFactory(p => GenerateQuotePage(p, state))
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .Build();

        return paginator;
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
            .WithUserAsAuthor(await message.GetUserAsync(client), AuthorOptions.IncludeId)
            .WithTimestamp(message.Timestamp)
            .AddJumpLink(message, executingUser);

        if (message.ReferencedMessageId is not null)
        {
            var reply = await logging.GetLatestMessage(
                message.GuildId, message.ChannelId,
                message.ReferencedMessageId.Value);
            var replyUser = await logging.GetUserAsync(reply);

            embed.WithMessageReference(message, reply, replyUser);
        }

        return new[] { embed }.Concat(message.ToEmbedBuilders(QuoteOptions | EmbedBuilderOptions.UseProxy));
    }

    private static IPage GenerateQuotePage(IComponentPaginator p, QuotePaginatorState state)
    {
        const uint accentColor = 0x9B59FF;

        var entry = state.Entries[p.CurrentPageIndex];

        var headerSection = new SectionBuilder().WithTextDisplay(entry.HeaderText);
        if (!string.IsNullOrWhiteSpace(entry.AuthorAvatarUrl))
            headerSection.WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(entry.AuthorAvatarUrl)));

        var container = new ContainerBuilder()
            .WithSection(headerSection)
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        if (entry.MediaItems.Count != 0)
            container.WithMediaGallery(entry.MediaItems);

        if (!string.IsNullOrWhiteSpace(entry.BodyText))
        {
            container.WithTextDisplay(entry.BodyText);
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        if (entry.AttachmentsText is { Length: > 0 })
        {
            container.WithTextDisplay(entry.AttachmentsText);
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        if (entry.CanShowModerationButtons)
        {
            container.WithActionRow(new ActionRowBuilder()
                .WithButton("Delete", $"delete:{entry.ChannelId}:{entry.MessageId}", ButtonStyle.Danger,
                    disabled: p.ShouldDisable())
                .WithButton("User Info", $"user:{entry.UserId}", ButtonStyle.Secondary, disabled: p.ShouldDisable())
                .WithButton("Reprimands", $"history:{entry.UserId}", ButtonStyle.Secondary, disabled: p.ShouldDisable()));
        }

        container.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary)
            .AddStopButton(p, "Close", ButtonStyle.Danger));

        container
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay(state.SourceJumpUrl is { Length: > 0 }
                ? $"-# Requested by {state.RequestedBy.Mention} • Source: [jump]({state.SourceJumpUrl})"
                : $"-# Requested by {state.RequestedBy.Mention}")
            .WithAccentColor(accentColor);

        var components = new ComponentBuilderV2().WithContainer(container).Build();
        return new PageBuilder()
            .WithComponents(components)
            .WithAllowedMentions(AllowedMentions.None)
            .Build();
    }

    private sealed record QuoteEntry(
        QuotedMessage Quote,
        ulong ChannelId,
        ulong MessageId,
        ulong UserId,
        string HeaderText,
        string BodyText,
        string? AttachmentsText,
        string? AuthorAvatarUrl,
        IReadOnlyList<MediaGalleryItemProperties> MediaItems,
        bool CanShowModerationButtons)
    {
        public static QuoteEntry FromMessage(
            QuotedMessage quote,
            IMessage message,
            IReadOnlyList<Embed> embeds,
            bool canModerate)
        {
            var header = BuildHeader(message.Author, quote.UserId, quote.ChannelId, message.Timestamp, message.GetJumpUrl());
            var body = BuildBody(message.Content);
            var (media, attachments) = BuildMediaAndAttachments(message.Attachments);

            var extraEmbeds = embeds
                .Select(e => e.Image?.Url)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Cast<string>()
                .ToList();

            foreach (var url in extraEmbeds)
            {
                if (media.Count >= 10) break;
                if (media.Any(m => m.Media.Url == url)) continue;
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
            }

            return new QuoteEntry(
                quote,
                quote.ChannelId,
                quote.MessageId,
                quote.UserId,
                header,
                body,
                attachments,
                message.Author.GetAvatarUrl(size: 256),
                media,
                canModerate);
        }

        public static QuoteEntry FromLog(
            QuotedMessage quote,
            MessageLog log,
            IReadOnlyList<Embed> embeds,
            bool canModerate)
        {
            var header = BuildHeader(null, quote.UserId, quote.ChannelId, log.Timestamp, log.GetJumpUrl()) + "\n-# *(message deleted)*";
            var body = BuildBody(log.Content);
            var (media, attachments) = BuildMediaAndAttachments(log.Attachments);

            var extraEmbeds = embeds
                .Select(e => e.Image?.Url)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Cast<string>()
                .ToList();

            foreach (var url in extraEmbeds)
            {
                if (media.Count >= 10) break;
                if (media.Any(m => m.Media.Url == url)) continue;
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
            }

            return new QuoteEntry(
                quote,
                quote.ChannelId,
                quote.MessageId,
                quote.UserId,
                header,
                body,
                attachments,
                null,
                media,
                canModerate);
        }

        private static (List<MediaGalleryItemProperties> Media, string? AttachmentsText) BuildMediaAndAttachments(
            IEnumerable<IAttachment> attachments)
        {
            var media = new List<MediaGalleryItemProperties>();
            var files = new List<string>();

            foreach (var attachment in attachments)
            {
                var url = attachment.Url;
                var isImage = attachment.Height is not null
                              || attachment.Width is not null
                              || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                              || url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                              || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                              || url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                              || url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);

                if (isImage && media.Count < 10)
                {
                    media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(url)));
                }
                else
                {
                    files.Add($"- [{attachment.Filename}]({url})");
                }
            }

            var text = files.Count == 0
                ? null
                : $"### Attachments\n{string.Join("\n", files.Take(8))}";

            return (media, text);
        }

        private static string BuildHeader(
            IUser? author,
            ulong userId,
            ulong channelId,
            DateTimeOffset timestamp,
            string jumpUrl)
        {
            var who = author is null ? $"<@{userId}>" : $"{author} (<@{author.Id}>)";
            return
                $"## Quote\n" +
                $"**Author:** {who}\n" +
                $"**Channel:** {MentionUtils.MentionChannel(channelId)}\n" +
                $"**Time:** <t:{timestamp.ToUnixTimeSeconds()}:f> (<t:{timestamp.ToUnixTimeSeconds()}:R>)\n" +
                $"**Jump:** [open]({jumpUrl})";
        }

        private static string BuildBody(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "### Content\n*No message content.*";

            var safe = FormatUtilities.SanitizeAllMentions(content);
            safe = safe.Length > 2500 ? $"{safe[..2500]}…" : safe;
            return $"### Content\n>>> {safe}";
        }

    }

    private sealed record QuotePaginatorState(
        IReadOnlyList<QuoteEntry> Entries,
        IUser RequestedBy,
        string SourceJumpUrl);
}