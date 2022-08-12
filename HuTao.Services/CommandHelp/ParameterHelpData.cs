using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Discord.Commands;
using HuTao.Services.Utilities;
using Namotion.Reflection;

namespace HuTao.Services.CommandHelp;

public class ParameterHelpData
{
    private IEnumerable<ParameterHelpData> _options = null!;

    private ParameterHelpData(
        string name, Type type,
        string? summary = null,
        bool isOptional = false)
    {
        Name       = name;
        Summary    = summary;
        Type       = type;
        IsOptional = isOptional;
    }

    public bool IsOptional { get; set; }

    public IEnumerable<ParameterHelpData> Options => LazyInitializer.EnsureInitialized(ref _options, () =>
    {
        var type = Type.GetGenericArguments().FirstOrDefault(Type);

        return type switch
        {
            var t when t.IsEnum                                                 => FromEnum(t),
            var t when t.GetAttribute<NamedArgumentTypeAttribute>() is not null => FromNamedArgumentInfo(t),
            _                                                                   => Enumerable.Empty<ParameterHelpData>()
        };
    });

    public string Name { get; set; }

    public string? Summary { get; set; }

    public Type Type { get; set; }

    public static ParameterHelpData FromParameterInfo(ParameterInfo parameter)
    {
        var type = parameter.Type.ToContextualType();
        return new ParameterHelpData(parameter.Name, type.Type, parameter.Summary,
            parameter.IsOptional || type.Nullability == Nullability.Nullable);
    }

    private static IEnumerable<ParameterHelpData> FromEnum(Type type) => type.GetEnumValues()
        .OfType<Enum>()
        .Select(e =>
        {
            var summary =
                e.GetAttributeOfEnum<HelpSummaryAttribute>()?.Text ??
                e.GetAttributeOfEnum<DescriptionAttribute>()?.Description;

            return new ParameterHelpData(e.ToString(), type, summary);
        });

    private static IEnumerable<ParameterHelpData> FromNamedArgumentInfo(Type type)
    {
        var properties = type.GetProperties();

        return properties.Select(p =>
        {
            var info = p.ToContextualProperty();
            return new ParameterHelpData(info.Name, info.PropertyType,
                info.GetContextAttribute<HelpSummaryAttribute>()?.Text,
                info.Nullability == Nullability.Nullable);
        });
    }
}