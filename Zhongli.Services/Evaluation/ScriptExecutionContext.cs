using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Services.Evaluation;

public record Globals(ConsoleLikeStringWriter Console, Context Context, IServiceProvider Services);

public class ScriptExecutionContext
{
    private static readonly List<string> DefaultImports =
        new()
        {
            "Discord",
            "Discord.Commands",
            "Discord.Interactions",
            "Discord.WebSocket",
            "Humanizer",
            "Humanizer.Localisation",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "System",
            "System.Collections",
            "System.Collections.Concurrent",
            "System.Collections.Immutable",
            "System.Collections.Generic",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Net",
            "System.Net.Http",
            "System.Numerics",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Text.Json"
        };

    private static readonly List<Assembly> DefaultReferences =
        new()
        {
            typeof(Enumerable).GetTypeInfo().Assembly,
            typeof(HttpClient).GetTypeInfo().Assembly,
            typeof(List<>).GetTypeInfo().Assembly,
            typeof(string).GetTypeInfo().Assembly,
            typeof(ValueTuple).GetTypeInfo().Assembly,
            typeof(Globals).GetTypeInfo().Assembly,
            typeof(CollectionHumanizeExtensions).GetTypeInfo().Assembly
        };

    private static readonly IEnumerable<string> AssemblyImports = Assembly
        .GetEntryAssembly()!.GetTypes()
        .Select(x => x.Namespace)
        .OfType<string>();

    private static readonly IEnumerable<Assembly> AssemblyReferences = AppDomain
        .CurrentDomain.GetAssemblies()
        .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location));

    public ScriptExecutionContext(string code) { Code = Regex.Replace(code, @"```\w*", string.Empty); }

    public ScriptOptions Options =>
        ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion.Preview)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithImports(Imports)
            .WithReferences(References);

    public string Code { get; set; }

    private HashSet<Assembly> References { get; } = new(DefaultReferences.Concat(AssemblyReferences));

    private HashSet<string> Imports { get; } = new(DefaultImports.Concat(AssemblyImports));

    public bool TryAddReferenceAssembly(Assembly? assembly)
    {
        if (assembly is null) return false;

        if (References.Contains(assembly)) return false;

        References.Add(assembly);
        return true;
    }

    public void AddImport(string import)
    {
        if (string.IsNullOrEmpty(import)) return;
        if (Imports.Contains(import)) return;

        Imports.Add(import);
    }
}