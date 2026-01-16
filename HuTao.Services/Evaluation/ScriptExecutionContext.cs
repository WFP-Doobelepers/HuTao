using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord.Webhook;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace HuTao.Services.Evaluation;

public record Globals(ConsoleLikeStringWriter Console, Context Context, IServiceProvider Services);

public class ScriptExecutionContext(string code)
{
    private static readonly List<string> DefaultImports =
    [
        "Discord",
        "Discord.Commands",
        "Discord.Interactions",
        "Discord.Webhook",
        "Discord.WebSocket",
        "Humanizer",
        "Microsoft.Extensions.DependencyInjection",
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
    ];

    private static readonly List<Assembly> BotAssemblies =
    [
        Assembly.GetEntryAssembly()!,
        typeof(HuTaoContext).Assembly,
        typeof(ScriptExecutionContext).Assembly,
        typeof(DiscordWebhookClient).Assembly
    ];

    private static readonly IEnumerable<string> AssemblyImports = BotAssemblies
        .SelectMany(a => a.GetTypes())
        .Select(x => x.Namespace)
        .OfType<string>();

    private static readonly IEnumerable<Assembly> AssemblyReferences = AppDomain
        .CurrentDomain.GetAssemblies()
        .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location));

    public ScriptOptions Options =>
        ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion.Preview)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithImports(Imports)
            .WithReferences(References);

    public string Code { get; set; } = Regex.Replace(code, @"```\w*", string.Empty);

    private HashSet<Assembly> References { get; } = [..AssemblyReferences];

    private HashSet<string> Imports { get; } = [..DefaultImports.Concat(AssemblyImports)];

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