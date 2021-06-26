using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace Zhongli.Services.Utilities
{
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
            if (splitLines.Any())
            {
                builder.AddField(title, splitLines.First());
                foreach (var line in splitLines.Skip(1))
                {
                    builder.AddField("\x200b", line);
                }
            }

            return builder;
        }

        public static EmbedBuilder WithUserAsAuthor(this EmbedBuilder builder, IUser user, string? extra = null)
        {
            var suffix = string.Empty;

            if (!string.IsNullOrWhiteSpace(extra)) suffix = $" ({extra})";

            return builder
                .WithAuthor(user.GetFullUsername() + suffix, user.GetDefiniteAvatarUrl());
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