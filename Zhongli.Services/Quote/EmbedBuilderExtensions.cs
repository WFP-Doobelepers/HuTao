using System.Linq;
using Discord;
using Humanizer.Bytes;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Quote
{
    public static class EmbedBuilderExtensions
    {
        public static bool TryAddImageAttachment(this EmbedBuilder embed, IMessage message)
        {
            var firstAttachment = message.Attachments.FirstOrDefault();
            if (firstAttachment?.Height is null)
                return false;

            embed.WithImageUrl(firstAttachment.Url);

            return true;
        }

        public static bool TryAddImageEmbed(this EmbedBuilder embed, IMessage message)
        {
            var imageEmbed = message.Embeds.Select(x => x.Image).FirstOrDefault(x => x is { });
            if (imageEmbed is null)
                return false;

            embed.WithImageUrl(imageEmbed.Value.Url);

            return true;
        }

        public static bool TryAddOtherAttachment(this EmbedBuilder embed, IMessage message)
        {
            var firstAttachment = message.Attachments.FirstOrDefault();
            if (firstAttachment is null) return false;

            embed.AddField($"Attachment (Size: {new ByteSize(firstAttachment.Size)})", firstAttachment.Url);

            return true;
        }

        public static bool TryAddThumbnailEmbed(this EmbedBuilder embed, IMessage message)
        {
            var thumbnailEmbed = message.Embeds.Select(x => x.Thumbnail).FirstOrDefault(x => x is { });
            if (thumbnailEmbed is null)
                return false;

            embed.WithImageUrl(thumbnailEmbed.Value.Url);

            return true;
        }

        public static EmbedBuilder AddActivity(this EmbedBuilder embed, IMessage message)
        {
            if (message.Activity is null) return embed;

            return embed
                .AddField("Invite Type", message.Activity.Type)
                .AddField("Party Id", message.Activity.PartyId);
        }

        public static EmbedBuilder AddContent(this EmbedBuilder embed, IMessage message)
            => embed.AddContent(message.Content);

        public static EmbedBuilder AddContent(this EmbedBuilder embed, string content)
            => string.IsNullOrWhiteSpace(content) ? embed : embed.WithDescription(content);

        public static EmbedBuilder AddJumpLink(this EmbedBuilder embed, IMessage message, IMentionable executingUser)
            => embed
                .AddField("Quoted by", executingUser.Mention, true)
                .AddField("Author", $"{message.Author.Mention} from {Format.Bold(message.GetJumpUrlForEmbed())}", true);

        public static EmbedBuilder AddJumpLink(this EmbedBuilder embed, IMessage message, bool useTitle = false)
            => useTitle
                ? embed.WithTitle($"From #{message.Channel.Name}").WithUrl(message.GetJumpUrl())
                : embed.AddField("Context",
                    $"By {message.Author.Mention} from {Format.Bold(message.GetJumpUrlForEmbed())}",
                    true);

        public static EmbedBuilder AddMeta(this EmbedBuilder embed, IMessage message,
            AuthorOptions options = AuthorOptions.None) => embed
            .WithUserAsAuthor(message.Author, options)
            .WithTimestamp(message.Timestamp);

        public static EmbedBuilder AddOtherEmbed(this EmbedBuilder embed, IMessage message) => message.Embeds.Count == 0
            ? embed
            : embed.AddField("Embed Type", message.Embeds.First().Type);

        public static EmbedBuilder? GetRichEmbed(this IMessage message, IMentionable executingUser)
        {
            var firstEmbed = message.Embeds.FirstOrDefault();
            if (firstEmbed?.Type != EmbedType.Rich) return null;

            var embed = message.Embeds
                .First()
                .ToEmbedBuilder();

            if (firstEmbed.Color is null) embed.Color = Color.DarkGrey;

            return embed;
        }
    }
}