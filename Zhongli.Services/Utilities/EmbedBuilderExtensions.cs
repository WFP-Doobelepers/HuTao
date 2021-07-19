using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace Zhongli.Services.Utilities
{
    [Flags]
    public enum AuthorOptions
    {
        None = 1 << 0,
        IncludeId = 1 << 1,
        UseFooter = 1 << 2,
        UseThumbnail = 1 << 3,
        Requested = 1 << 4
    }

    public static class EmbedBuilderExtensions
    {
        private const int FieldMaxSize = 1024;

        public static EmbedBuilder AddLinesIntoFields<T>(this EmbedBuilder builder, string title,
            IEnumerable<T> lines, Func<T, string> lineSelector) =>
            builder.AddLinesIntoFields(title, lines.Select(lineSelector));

        public static EmbedBuilder AddLinesIntoFields<T>(this EmbedBuilder builder, string title,
            IEnumerable<T> lines, Func<T, int, string> lineSelector) =>
            builder.AddLinesIntoFields(title, lines.Select(lineSelector));

        public static EmbedBuilder AddLinesIntoFields(this EmbedBuilder builder, string title,
            IEnumerable<string> lines)
        {
            var splitLines = SplitLinesIntoChunks(lines).ToArray();

            if (!splitLines.Any()) return builder;

            builder.AddField(title, splitLines.First());
            foreach (var line in splitLines.Skip(1))
            {
                builder.AddField("\x200b", line);
            }

            return builder;
        }

        public static EmbedBuilder WithUserAsAuthor(this EmbedBuilder embed, IUser user,
            AuthorOptions authorOptions = AuthorOptions.None)
        {
            var username = user.GetFullUsername();

            if (authorOptions.HasFlag(AuthorOptions.IncludeId))
                username += $" ({user.Id})";

            if (authorOptions.HasFlag(AuthorOptions.Requested))
                username = $"Requested by {username}";

            var avatar = user.GetDefiniteAvatarUrl();

            if (authorOptions.HasFlag(AuthorOptions.UseThumbnail))
                embed.WithThumbnailUrl(avatar);

            return authorOptions.HasFlag(AuthorOptions.UseFooter)
                ? embed.WithFooter(username, avatar)
                : embed.WithAuthor(username, avatar);
        }

        public static IEnumerable<string> SplitLinesIntoChunks(this IEnumerable<string> lines,
            int maxLength = FieldMaxSize)
        {
            var sb = new StringBuilder(0, maxLength);
            var builders = new List<StringBuilder>();

            foreach (var line in lines)
            {
                if (sb.Length + Environment.NewLine.Length + line.Length > maxLength)
                {
                    builders.Add(sb);
                    sb = new StringBuilder(0, maxLength);
                }

                sb.AppendLine(line);
            }

            builders.Add(sb);

            return builders
                .Where(s => s.Length > 0)
                .Select(s => s.ToString());
        }
    }
}