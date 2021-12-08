using System;
using System.Collections.Concurrent;

namespace Zhongli.Services.Utilities;

public class DictionaryCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _cachedItems = new();
    private readonly Func<TKey, TValue> _func;

    public DictionaryCache(Func<TKey, TValue> func) { _func = func; }

    public TValue this[TKey key]
    {
        get
        {
            if (_cachedItems.TryGetValue(key, out var value))
                return value;

            var val = _func(key);
            _cachedItems[key] = val;
            return val;
        }
    }
}