using System;
using System.Collections.Generic;
using Discord.Commands;

namespace Zhongli.Services.Interactive
{
    public class PromptResult
    {
        public PromptResult(string question, object? userResponse)
        {
            Question     = question;
            UserResponse = userResponse;
        }

        public object? UserResponse { get; }

        public string Question { get; }

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

        public static TValue GetOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, PromptResult> results, TKey key, TValue @default = default)
            where TKey : notnull
        {
            if (results.TryGetValue(key, out var result) && result.UserResponse is TValue)
                return result.As<TValue>();

            return @default;
        }
    }
}