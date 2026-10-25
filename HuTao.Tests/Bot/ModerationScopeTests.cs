using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using Xunit;
using CommandAttribute = Discord.Commands.CommandAttribute;
using ModalInteractionAttribute = Discord.Interactions.ModalInteractionAttribute;
using SlashCommandAttribute = Discord.Interactions.SlashCommandAttribute;

namespace HuTao.Tests.Bot;

// A moderator can take each moderation action with the text command, the slash command, the slash menu command, and the
// form that the mod menu opens. The mod menu opens the form for a moderator with any moderation scope, so every one of
// these ways must ask for the action's own scope, or a moderator without it can take the action another way. A command
// that undoes an action has only the text and slash commands, and they ask for the scope of the action they undo.
public class ModerationScopeTests
{
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly;

    private static readonly string[] Checks =
        ["RequireAuthorizationAttribute", "CheckCategoryAttribute", "RequireAnyCategoryAuthorizationAttribute"];

    // The text command, the slash command, the form's custom ID, and the scope of the action.
    public static TheoryData<string, string, string?, AuthorizationScope> Actions => new()
    {
        { "ban", "ban", "ban:*", AuthorizationScope.Ban },
        { "hardmute", "hardmute", "hardMute:*", AuthorizationScope.HardMute },
        { "kick", "kick", "kick:*", AuthorizationScope.Kick },
        { "mute", "mute", "mute:*", AuthorizationScope.Mute },
        { "note", "note", "note:*", AuthorizationScope.Note },
        { "notice", "notice", "notice:*", AuthorizationScope.Warning },
        { "timeout", "timeout", "timeout:*", AuthorizationScope.Timeout },
        { "warn", "warn", "warn:*", AuthorizationScope.Warning },
        { "unban", "unban", null, AuthorizationScope.Ban },
        { "unmute", "unmute", null, AuthorizationScope.Mute },
        { "untimeout", "untimeout", null, AuthorizationScope.Timeout }
    };

    [Theory]
    [MemberData(nameof(Actions))]
    public void EveryWayToTakeAnAction_AsksForTheActionsScope(
        string command, string slash, string? form, AuthorizationScope scope)
    {
        var ways = Ways(typeof(ModerationModule), m => m.GetCustomAttribute<CommandAttribute>()?.Text == command)
            .Concat(Ways(typeof(InteractiveModerationModule),
                m => m.GetCustomAttribute<SlashCommandAttribute>()?.Name is { } name && (name == slash || name == $"{slash}-menu")))
            .Concat(Ways(typeof(InteractiveModerationModule),
                m => form is not null && m.GetCustomAttribute<ModalInteractionAttribute>()?.CustomId == form))
            .ToList();

        // The text overloads and the slash command, and for an action, also the menu command and the form.
        Assert.True(ways.Count >= 4, $"Only {ways.Count} ways to {command} were found.");
        Assert.All(ways, way => Assert.True(way.Scopes.SequenceEqual([scope]),
            $"{way.Name} asks for [{string.Join(", ", way.Scopes)}] instead of {scope}."));
    }

    private static IEnumerable<(string Name, AuthorizationScope[] Scopes)> Ways(Type module, Func<MethodInfo, bool> match)
        => module.GetMethods(Methods).Where(match).Select(m => ($"{module.Name}.{m.Name}", Scopes(m)));

    // The scope that each check on the method or on its parameters asks for.
    private static AuthorizationScope[] Scopes(MethodInfo method)
        => method.GetCustomAttributesData()
            .Concat(method.GetParameters().SelectMany(p => p.GetCustomAttributesData()))
            .Where(a => Checks.Contains(a.AttributeType.Name))
            .Select(a => (AuthorizationScope) (int) a.ConstructorArguments[0].Value!)
            .Distinct()
            .ToArray();
}
