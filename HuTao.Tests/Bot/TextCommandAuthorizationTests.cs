using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Bot.Modules;
using HuTao.Services.Core.Preconditions.Commands;
using Xunit;

namespace HuTao.Tests.Bot;

/// <summary>
///     Discord.Commands runs no global authorization, so a text command without a precondition can be run by every
///     member of the guild. These tests walk every text command the bot registers and fail on any that is unguarded.
/// </summary>
public class TextCommandAuthorizationTests
{
    /// <summary>
    ///     Text commands that may run without an authorization precondition, keyed by <c>Module.Method</c>, with the
    ///     reason each one is safe to leave open.
    /// </summary>
    private static readonly Dictionary<string, string> PublicCommands = new()
    {
        ["HelpModule.HelpAsync"] = "Help is for everyone.",
        ["HelpModule.HelpCommandAsync"] = "Help is for everyone.",
        ["HelpModule.HelpDMAsync"] = "Help is for everyone.",
        ["HelpModule.HelpModuleAsync"] = "Help is for everyone.",
        ["GeneralModule.PingAsync"] = "Read-only latency check.",
        ["GenshinCommands.CodesAsync"] = "Read-only; posting elsewhere checks the user can send in that channel.",
        ["GenshinCommands.LivestreamAsync"] = "Read-only countdown.",
        ["GenshinCommands.VersionsCommand"] = "Read-only schedule.",
        ["VoiceChatModule.BanAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.ClaimAsync"] = "Members claim a voice chat once its owner has left.",
        ["VoiceChatModule.HideAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.KickAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.LimitAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.LockAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.OwnerAsync"] = "Read-only.",
        ["VoiceChatModule.RevealAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.TransferAsync"] = "Only the owner, or someone who can manage the channel, can use it.",
        ["VoiceChatModule.UnbanAsync"] = "Only the owner of the voice chat can use it.",
        ["VoiceChatModule.UnlockAsync"] = "Only the owner of the voice chat can use it.",
        ["ModerationModule.TemplateAsync"] =
            "Checked in the command against the template's own scope, which no static precondition can express."
    };

    private static readonly Type[] AuthorizationPreconditions =
    [
        typeof(RequireAuthorizationAttribute),
        typeof(RequireCategoryAuthorizationAttribute),
        typeof(RequireTeamMemberAttribute),
        typeof(RequireUserPermissionAttribute),
        typeof(RequireOwnerAttribute),

        // Private to the Genshin module: team members, plus the users and roles it allows.
        typeof(GenshinModule).GetNestedType("RequireAllowedUserCommandAttribute", BindingFlags.NonPublic)!
    ];

    [Fact]
    public void EveryTextCommand_HasAnAuthorizationPrecondition()
    {
        var unguarded = TextCommands()
            .Where(c => !c.IsGuarded && !PublicCommands.ContainsKey(c.Key))
            .Select(c => $"{c.Key} ({c.Path})")
            .Distinct()
            .Order()
            .ToList();

        Assert.True(unguarded.Count == 0,
            $"Text commands without an authorization precondition:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unguarded));
    }

    [Fact]
    public void PublicCommands_AreUnguardedCommandsThatExist()
    {
        var commands = TextCommands().ToList();
        var stale = PublicCommands.Keys
            .Where(key => !commands.Any(c => c.Key == key && !c.IsGuarded))
            .Order()
            .ToList();

        Assert.True(stale.Count == 0,
            $"Allow-list entries that no longer match an unguarded command:{Environment.NewLine}"
            + string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void TextCommands_AreDiscovered()
    {
        var commands = TextCommands().ToList();

        // Inherited commands must be found, or the walk would miss the ones this test exists for.
        Assert.Contains(commands, c => c.Path == "auto delete");
        Assert.Contains(commands, c => c.Path == "censor toggle");
        Assert.Contains(commands, c => c.Path == "category add");
    }

    /// <summary>
    ///     Mirrors how Discord.Net 3.18 builds modules (<c>ModuleClassBuilder</c>): public non-abstract
    ///     <see cref="IModuleBase" /> types, their nested modules, and every instance method with a
    ///     <see cref="CommandAttribute" />, including inherited ones. Module preconditions include those of the
    ///     base classes and of the enclosing modules.
    /// </summary>
    private static IEnumerable<TextCommand> TextCommands()
    {
        var assembly = typeof(HelpModule).Assembly;
        var modules = assembly.DefinedTypes
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .Where(t => IsModule(t) && !t.IsDefined(typeof(DontAutoLoadAttribute)))
            .Where(t => t.DeclaringType is null || !IsModule(t.DeclaringType));

        return modules.SelectMany(m => Commands(m, [], []));
    }

    private static IEnumerable<TextCommand> Commands(
        Type module, IReadOnlyList<PreconditionAttribute> parentPreconditions, IReadOnlyList<string> parentGroups)
    {
        var preconditions = parentPreconditions
            .Concat(module.GetCustomAttributes<PreconditionAttribute>(true))
            .ToList();

        var group = module.GetCustomAttribute<GroupAttribute>(true)?.Prefix;
        var groups = string.IsNullOrEmpty(group) ? parentGroups : [..parentGroups, group];

        var methods = module
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.IsDefined(typeof(CommandAttribute)))
            .Where(m => m.ReturnType == typeof(Task) || m.ReturnType == typeof(Task<RuntimeResult>))
            .Where(m => !m.IsStatic && !m.IsGenericMethod);

        foreach (var method in methods)
        {
            var text = method.GetCustomAttribute<CommandAttribute>()?.Text;
            var path = string.Join(' ', string.IsNullOrEmpty(text) ? groups : [..groups, text]);
            var all = preconditions.Concat(method.GetCustomAttributes<PreconditionAttribute>(true));
            var guarded = IsGuarded(all) || HasCategoryCheck(method);

            yield return new TextCommand($"{module.Name}.{method.Name}", path, guarded);
        }

        foreach (var nested in module.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Where(IsModule))
        {
            foreach (var command in Commands(nested, preconditions, groups))
                yield return command;
        }
    }

    /// <summary>
    ///     Ungrouped preconditions must all pass, while a named group passes when any one of its members passes. So a
    ///     command is guarded by an ungrouped authorization precondition, or by a group made only of them.
    /// </summary>
    private static bool IsGuarded(IEnumerable<PreconditionAttribute> preconditions)
        => preconditions
            .GroupBy(p => p.Group, StringComparer.Ordinal)
            .Any(g => g.Key is null ? g.Any(IsAuthorization) : g.All(IsAuthorization));

    private static bool IsAuthorization(PreconditionAttribute precondition)
        => AuthorizationPreconditions.Contains(precondition.GetType());

    /// <summary>
    ///     <see cref="CheckCategoryAttribute" /> asks the authorization service whether the user may use the given
    ///     moderation category, which falls back to the guild's rules when no category is given.
    /// </summary>
    private static bool HasCategoryCheck(MethodInfo method)
        => method.GetParameters().Any(p => p.IsDefined(typeof(CheckCategoryAttribute)));

    private static bool IsModule(Type type)
        => typeof(IModuleBase).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters;

    private record TextCommand(string Key, string Path, bool IsGuarded);
}
