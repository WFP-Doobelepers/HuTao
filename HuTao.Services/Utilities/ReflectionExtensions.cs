using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HuTao.Services.CommandHelp;

namespace HuTao.Services.Utilities;

public static class ReflectionExtensions
{
    private static readonly DictionaryCache<Type, IReadOnlyCollection<PropertyInfo>> CachedPrimitives =
        new(GetPrimitives);

    private static readonly DictionaryCache<Type, IReadOnlyCollection<PropertyInfo>> CachedLists =
        new(GetLists);

    private static readonly DictionaryCache<Type, IReadOnlyCollection<PropertyInfo>> CachedProperties =
        new(GetPublicProperties);

    private static readonly DictionaryCache<Type, Type> TypeCache = new(GetRealTypeInternal);

    private static readonly DictionaryCache<(MemberInfo, Type), Attribute?> AttributeCache =
        new DictionaryCache<(MemberInfo Member, Type Type), Attribute?>(GetAttributeFromMember);

    private static readonly DictionaryCache<(Enum, Type), Attribute?> EnumAttributeCache =
        new(GetAttributeFromEnum);

    private static readonly IEnumerable<Type> EnumerableTypes = new[]
    {
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IReadOnlyCollection<>),
        typeof(List<>),
        typeof(IList<>),
        typeof(IReadOnlyList<>)
    };

    private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, object>> WeakCache = new();

    public static bool IsEnumerableOfT(this Type type)
        => type.IsGenericType && EnumerableTypes.Contains(type.GetGenericTypeDefinition());

    public static IReadOnlyCollection<PropertyInfo> GetLists<T>() => CachedLists[typeof(T)];

    public static IReadOnlyCollection<PropertyInfo> GetPrimitives<T>() => CachedPrimitives[typeof(T)];

    public static IReadOnlyCollection<PropertyInfo> GetProperties<T>() => CachedProperties[typeof(T)];

    public static T? GetAttribute<T>(this MemberInfo member) where T : Attribute
        => AttributeCache[(member, typeof(T))] as T;

    public static T? GetAttributeOfEnum<T>(this Enum obj) where T : Attribute
        => EnumAttributeCache[(obj, typeof(T))] as T;

    public static TResult Memoized<T, TResult>(
        this object context, T arg, Func<T, TResult> f,
        [CallerMemberName] string? cacheKey = null) where T : notnull
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (cacheKey == null) throw new ArgumentNullException(nameof(cacheKey));

        var objCache = WeakCache.GetOrCreateValue(context);

        var methodCache = (ConcurrentDictionary<T, TResult>) objCache
            .GetOrAdd(cacheKey, _ => new ConcurrentDictionary<T, TResult>());

        return methodCache.GetOrAdd(arg, f);
    }

    public static Type GetRealType(this Type type) => TypeCache[type];

    public static Type GetRealType(this PropertyInfo property) => TypeCache[property.PropertyType];

    public static Type GetRealType(this ParameterHelpData type) => TypeCache[type.Type];

    private static Attribute? GetAttributeFromEnum((Enum @enum, Type attribute) o)
    {
        var (@enum, attribute) = o;
        var enumType = @enum.GetType();
        var name = Enum.GetName(enumType, @enum);
        if (name is null)
            return null;

        var field = enumType.GetField(name);
        return field is null ? null : GetAttributeFromMember((field, attribute));
    }

    private static Attribute? GetAttributeFromMember((MemberInfo Member, Type Type) o) =>
        o.Member.GetCustomAttribute(o.Type);

    private static IReadOnlyCollection<PropertyInfo> GetLists(Type t) => CachedProperties[t]
        .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        .ToArray();

    private static IReadOnlyCollection<PropertyInfo> GetPrimitives(Type t) => CachedProperties[t]
        .Where(p => !p.PropertyType.IsGenericType)
        .ToArray();

    private static IReadOnlyCollection<PropertyInfo> GetPublicProperties(this Type t)
        => t.GetProperties().ToArray();

    private static Type GetRealTypeInternal(Type type) =>
        type.IsGenericType
            ? type.GetGenericArguments()[0]
            : type;
}