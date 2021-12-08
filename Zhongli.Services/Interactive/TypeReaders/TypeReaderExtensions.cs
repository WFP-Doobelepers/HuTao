using System;
using Discord.Commands;
using Zhongli.Services.Core.TypeReaders;
using Zhongli.Services.Interactive.TryParse;

namespace Zhongli.Services.Interactive.TypeReaders;

public static class TypeReaderExtensions
{
    public static EnumTryParseTypeReader<T> AsTypeReader<T>(this EnumTryParseDelegate<T> tryParse,
        bool ignoreCase = true) where T : struct, Enum =>
        new(ignoreCase);

    public static TryParseTypeReader<T> AsTypeReader<T>(this TryParseDelegate<T> tryParse) =>
        new(tryParse);

    public static TypeReaderCriterion AsCriterion(this TypeReader reader, IServiceProvider? services = null) =>
        new(reader, services);
}