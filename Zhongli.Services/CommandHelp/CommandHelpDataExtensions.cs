using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Zhongli.Data.Config;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.CommandHelp
{
    public static class CommandHelpDataExtensions
    {
        public static EmbedBuilder AddCommandFields(this EmbedBuilder embed, CommandHelpData command)
        {
            var builder = new StringBuilder(command.Summary ?? "No summary.").AppendLine();
            var name = command.Aliases.FirstOrDefault();

            builder
                .AppendAliases(command.Aliases.Where(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList())
                .AppendParameters(command.Parameters);

            var prefix = ZhongliConfig.Configuration.Prefix;
            embed.AddField(new EmbedFieldBuilder()
                .WithName($"Command: {prefix}{name} {GetParams(command)}")
                .WithValue(builder.ToString()));

            return embed;
        }

        private static string GetParams(this CommandHelpData info)
        {
            var sb = new StringBuilder();

            var parameterInfo = info.Parameters.Select(p => Format.Code(GetParamName(p)));
            sb.Append(string.Join(" ", parameterInfo));

            return sb.ToString();
        }

        private static string GetParamName(ParameterHelpData parameter)
        {
            static string Surround(string text, bool isNullable) => isNullable ? $"[{text}]" : $"<{text}>";

            if (parameter.Type.IsEnum)
            {
                var parameters = parameter.Options.Select(p => p.Name);
                return Surround(string.Join("|", parameters), parameter.IsOptional);
            }

            return Surround(parameter.Name, parameter.IsOptional);
        }

        private static StringBuilder AppendAliases(this StringBuilder builder, IReadOnlyCollection<string> aliases)
        {
            if (aliases.Count == 0)
                return builder;

            builder.AppendLine(Format.Bold("Aliases:"));

            foreach (var alias in FormatUtilities.CollapsePlurals(aliases))
            {
                builder.AppendLine($"• {alias}");
            }

            return builder;
        }

        private static StringBuilder AppendParameters(this StringBuilder builder,
            IReadOnlyCollection<ParameterHelpData>? parameters)
        {
            if (parameters is null || parameters.Count == 0)
                return builder;

            if (parameters.HasSummary())
            {
                builder
                    .AppendLine(Format.Bold("Parameters:"))
                    .AppendSummaries(parameters);
            }

            foreach (var parameter in parameters.Where(p => p.Options.HasSummary()))
            {
                builder
                    .AppendLine()
                    .AppendLine($"Flags for {Format.Underline(Format.Bold(parameter.Name))}:")
                    .AppendSummaries(parameter.Options);
            }

            return builder;
        }

        private static bool HasSummary(this IEnumerable<ParameterHelpData> parameters) =>
            parameters.Any(p => !string.IsNullOrWhiteSpace(p.Summary));

        private static StringBuilder AppendSummaries(this StringBuilder builder,
            IEnumerable<ParameterHelpData> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (string.IsNullOrEmpty(parameter.Summary))
                    continue;

                builder.AppendLine($"\x200b\t• `{parameter.Name}`: {parameter.Summary}");
            }

            return builder;
        }
    }
}