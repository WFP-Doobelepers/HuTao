using System;
using System.Collections.Concurrent;

namespace HuTao.Services.Utilities;

public class DictionaryCache<TKey, TValue>(Func<TKey, TValue> func)
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _cachedItems = new();

    public TValue this[TKey key]
    {
        get
        {
            if (_cachedItems.TryGetValue(key, out var value))
                return value;

            var val = func(key);
            _cachedItems[key] = val;
            return val;
        }
    }
}