using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Discord.Commands;

namespace Zhongli.Services.Interactive
{
    public class ResultDictionary<TOptions> : ReadOnlyDictionary<TOptions, PromptResult> where TOptions : notnull
    {
        public ResultDictionary(IDictionary<TOptions, PromptResult> dictionary) : base(dictionary) { }

        public TValue Get<TValue>(TOptions key) => this[key].As<TValue>();

        public TValue Get<TValue>(TOptions key, Func<TypeReaderResult, TValue> selector) => this[key].As(selector);

        public TValue? GetOrDefault<TValue>(TOptions key, TValue? @default = default)
        {
            if (TryGetValue(key, out var result) && result!.UserResponse is TValue)
                return result.As<TValue>();

            return @default;
        }
    }
}