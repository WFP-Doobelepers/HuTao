using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Discord.Commands;

namespace HuTao.Services.Interactive;

public class PromptResult(string question, object? userResponse)
{
    public object? UserResponse { get; } = userResponse;

    public string Question { get; } = question;

    public T As<T>()
    {
        if (UserResponse is TypeReaderResult result)
            return (T) result.BestMatch;

        return (T) UserResponse!;
    }

    public T As<T>(Func<TypeReaderResult, T> selector) => selector((TypeReaderResult) UserResponse!);

    public static implicit operator string?(PromptResult? result) => result?.UserResponse?.ToString();
}

public static class PromptResultExtensions
{
    public static TValue Get<TKey, TValue>(
        this IReadOnlyDictionary<TKey, PromptResult> results, TKey key, Func<TypeReaderResult, TValue> selector)
        where TKey : notnull =>
        results[key].As(selector);

    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier")]
    public static TValue GetOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, PromptResult> results, TKey key, TValue @default)
        where TKey : notnull
    {
        if (results.TryGetValue(key, out var result) && result.UserResponse is TValue)
            return result.As<TValue>();

        return @default;
    }
}