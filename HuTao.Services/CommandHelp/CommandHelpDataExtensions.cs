using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Humanizer;
using HuTao.Data.Config;
using HuTao.Services.Utilities;

namespace HuTao.Services.CommandHelp;

public static class CommandHelpDataExtensions
{
    public static EmbedBuilder ToEmbedBuilder(this CommandHelpData command)
    {
        var embed = new EmbedBuilder();
        var builder = new StringBuilder(command.Summary ?? "No summary.").AppendLine();
        var name = command.Aliases.FirstOrDefault();

        builder
            .AppendAliases(command.Aliases
                .Where(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList())
            .AppendParameters(command.Parameters);

        var prefix = HuTaoConfig.Configuration.Prefix;

        var lines = builder.ToString().Split(Environment.NewLine);
        embed.AddItemsIntoFields($"Command: {prefix}{name} {GetParams(command)}", lines);

        return embed;
    }

    private static bool HasSummary(this IEnumerable<ParameterHelpData> parameters) =>
        parameters.Any(p => !string.IsNullOrWhiteSpace(p.Summary));

    private static string GetParamName(ParameterHelpData parameter) => parameter.GetRealType().IsEnum
        ? Surround(parameter.Name, parameter.IsOptional)
        : Surround($"{parameter.Name}: {parameter.GetParamTypeName()}", parameter.IsOptional);

    private static string GetParams(this CommandHelpData info)
    {
        var sb = new StringBuilder();

        var parameterInfo = info.Parameters
            .Select(p => Format.Code(GetParamName(p)));

        sb.Append(string.Join(" ", parameterInfo));

        return sb.ToString();
    }

    private static string GetParamTypeName(this ParameterHelpData parameter)
        => parameter.Type.IsEnumerableOfT()
            ? $"{parameter.GetRealType().Name} [...]"
            : parameter.GetRealType().Name;

    private static string Surround(string text, bool isNullable) => isNullable ? $"[{text}]" : $"<{text}>";

    private static StringBuilder AppendAliases(this StringBuilder builder, IReadOnlyCollection<string> aliases)
    {
        if (aliases.Count == 0)
            return builder;

        builder.AppendLine(Format.Bold("Aliases:"))
            .AppendLine(FormatUtilities.CollapsePlurals(aliases).Humanize(a => Format.Code(a)));

        return builder;
    }

    private static StringBuilder AppendParameter(this StringBuilder builder,
        ParameterHelpData parameter, ISet<Type> seenTypes)
    {
        if (!parameter.Options.Any() || seenTypes.Contains(parameter.Type))
            return builder;

        seenTypes.Add(parameter.Type);

        builder
            .AppendLine()
            .AppendLine($"Arguments for {Format.Underline(Format.Bold(parameter.Name))}:");

        if (parameter.GetRealType().IsEnum)
        {
            var names = parameter.Options.Select(p => p.Name);
            var values = Surround(string.Join("|\x200b", names), parameter.IsOptional);
            builder
                .AppendLine(Format.Code(values))
                .AppendSummaries(parameter.Options, true);
        }
        else
        {
            builder
                .AppendSummaries(parameter.Options.OrderBy(o => o.Name), false)
                .AppendLine(
                    $"▌Provide values by doing {Format.Code("name: value")} " +
                    $"or {Format.Code("name: \"value with spaces\"")}.");

            foreach (var nestedParameter in parameter.Options)
            {
                builder.AppendParameter(nestedParameter, seenTypes);
            }
        }

        return builder;
    }

    private static StringBuilder AppendParameters(this StringBuilder builder,
        IReadOnlyCollection<ParameterHelpData> parameters)
    {
        if (parameters.Count == 0)
            return builder;

        if (parameters.HasSummary())
        {
            builder
                .AppendLine(Format.Bold("Parameters:"))
                .AppendSummaries(parameters, true);
        }

        var seenTypes = new HashSet<Type>();
        foreach (var parameter in parameters)
        {
            builder.AppendParameter(parameter, seenTypes);
        }

        return builder;
    }

    private static StringBuilder AppendSummaries(this StringBuilder builder,
        IEnumerable<ParameterHelpData> parameters, bool hideEmpty)
    {
        foreach (var parameter in parameters)
        {
            if (string.IsNullOrEmpty(parameter.Summary) && hideEmpty)
                continue;

            var name = Format.Code(GetParamName(parameter));
            builder.AppendLine($"\x200b\t• {name}: {parameter.Summary ?? "No Summary."}");
        }

        return builder;
    }
}