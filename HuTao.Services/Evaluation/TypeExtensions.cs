using System;
using System.Collections.Generic;
using System.Linq;

namespace HuTao.Services.Evaluation;

public static class TypeExtensions
{
    private const string ArrayBrackets = "[]";

    private static readonly Dictionary<string, string> PrimitiveTypeNames = new()
    {
        ["Boolean"] = "bool",
        ["Byte"]    = "byte",
        ["Char"]    = "char",
        ["Decimal"] = "decimal",
        ["Double"]  = "double",
        ["Int16"]   = "short",
        ["Int32"]   = "int",
        ["Int64"]   = "long",
        ["SByte"]   = "sbyte",
        ["Single"]  = "float",
        ["String"]  = "string",
        ["UInt16"]  = "ushort",
        ["UInt32"]  = "uint",
        ["UInt64"]  = "ulong",
        ["Object"]  = "object"
    };

    public static string ParseGenericArgs(this Type type)
    {
        var generic = type.GetGenericArguments();

        if (generic.Length == 0) return GetPrimitiveTypeName(type);

        var name = type.Name;
        var args = generic.Select(a => a.ParseGenericArgs());
        return name.Replace($"`{generic.Length}", $"<{string.Join(", ", args)}>");
    }

    private static string GetPrimitiveTypeName(Type type)
    {
        var name = type.Name;
        if (type.IsArray) name = name.Replace(ArrayBrackets, string.Empty);

        if (!PrimitiveTypeNames.TryGetValue(name, out var primitive) || string.IsNullOrEmpty(primitive))
            return name;

        return type.IsArray
            ? string.Join(string.Empty, primitive, ArrayBrackets)
            : primitive;
    }
}