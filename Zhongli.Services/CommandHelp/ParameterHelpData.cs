using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Namotion.Reflection;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.CommandHelp
{
    public enum ParameterType
    {
        None,
        Enum,
        NamedArgument
    }

    public class ParameterHelpData
    {
        private ParameterHelpData(
            string name, Type type,
            string? summary = null, bool isOptional = false,
            IReadOnlyCollection<ParameterHelpData>? options = null)
        {
            Name       = name;
            Summary    = summary;
            Type       = type;
            IsOptional = isOptional;
            Options    = options ?? Array.Empty<ParameterHelpData>();
        }

        public bool IsOptional { get; set; }

        public IReadOnlyCollection<ParameterHelpData> Options { get; set; }

        public string Name { get; set; }

        public string? Summary { get; set; }

        public Type Type { get; set; }

        public static ParameterHelpData FromParameterInfo(ParameterInfo parameter)
        {
            var type = parameter.Type.ToContextualType();

            var options = type switch
            {
                var t when t.Type.IsEnum => FromEnum(t),
                var t when t.GetAttribute<NamedArgumentTypeAttribute>() is not null =>
                    FromNamedArgumentInfo(type),
                _ => null
            };

            return new ParameterHelpData(parameter.Name, type, parameter.Summary,
                parameter.IsOptional, options?.ToList());
        }

        private static IEnumerable<ParameterHelpData> FromEnum(Type type)
        {
            foreach (Enum? n in type.GetEnumValues())
            {
                if (n is null)
                    continue;

                var name = n.ToString();
                var summary = n.GetAttributeOfEnum<HelpSummaryAttribute>()?.Text;
                yield return new ParameterHelpData(name, type, summary);
            }
        }

        private static IEnumerable<ParameterHelpData> FromNamedArgumentInfo(CachedType type)
        {
            var properties = type.Type.GetPublicProperties();

            return properties.Select(p =>
            {
                var info = p.ToContextualProperty();
                return new ParameterHelpData(info.Name, info,
                    p.GetAttribute<HelpSummaryAttribute>()?.Text, info.Nullability == Nullability.Nullable);
            });
        }
    }
}