using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Discord.Commands;

namespace HuTao.Services.Interactive;

public class ResultDictionary<TOptions>(IDictionary<TOptions, PromptResult> dictionary)
    : ReadOnlyDictionary<TOptions, PromptResult>(dictionary)
    where TOptions : notnull
{
    public TValue Get<TValue>(TOptions key) => this[key].As<TValue>();

    public TValue Get<TValue>(TOptions key, Func<TypeReaderResult, TValue> selector) => this[key].As(selector);

    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier")]
    public TValue? GetOrDefault<TValue>(TOptions key, TValue? @default = default)
    {
        if (TryGetValue(key, out var result) && result.UserResponse is TValue)
            return result.As<TValue>();

        return @default;
    }
}