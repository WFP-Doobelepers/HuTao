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
        None         = 0,
        IncludeId    = 1 << 0,
        UseFooter    = 1 << 1,
        UseThumbnail = 1 << 2,
        Requested    = 1 << 3
    }

    public static class EmbedBuilderExtensions
    {
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

        public static EmbedBuilder WithGuildAsAuthor(this EmbedBuilder embed, IGuild guild,
            AuthorOptions authorOptions = AuthorOptions.None)
        {
            var name = guild.Name;
            if (authorOptions.HasFlag(AuthorOptions.Requested))
                name = $"Requested from {name}";

            return embed.WithEntityAsAuthor(guild, name, guild.IconUrl, authorOptions);
        }

        public static EmbedBuilder WithUserAsAuthor(this EmbedBuilder embed, IUser user,
            AuthorOptions authorOptions = AuthorOptions.None, ushort size = 128)
        {
            var username = user.GetFullUsername();
            if (authorOptions.HasFlag(AuthorOptions.Requested))
                username = $"Requested by {username}";

            return embed.WithEntityAsAuthor(user, username, user.GetDefiniteAvatarUrl(size), authorOptions);
        }

        public static IEnumerable<string> SplitLinesIntoChunks(this IEnumerable<string> lines,
            int maxLength = EmbedFieldBuilder.MaxFieldValueLength)
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

        private static EmbedBuilder WithEntityAsAuthor(this EmbedBuilder embed, IEntity<ulong> entity,
            string name, string iconUrl, AuthorOptions authorOptions)
        {
            if (authorOptions.HasFlag(AuthorOptions.IncludeId))
                name += $" ({entity.Id})";

            if (authorOptions.HasFlag(AuthorOptions.UseThumbnail))
                embed.WithThumbnailUrl(iconUrl);

            return authorOptions.HasFlag(AuthorOptions.UseFooter)
                ? embed.WithFooter(name, iconUrl)
                : embed.WithAuthor(name, iconUrl);
        }
    }
}