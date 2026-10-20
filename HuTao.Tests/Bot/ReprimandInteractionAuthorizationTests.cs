using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.Interactions;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using Xunit;

namespace HuTao.Tests.Bot;

/// <summary>
///     The reprimand handlers check the reprimand's own category only after looking it up, and the lookup already shows
///     the reprimand. So each handler that takes a reprimand ID must refuse members before it runs.
/// </summary>
public class ReprimandInteractionAuthorizationTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void ReprimandHandlers_RequireHistoryOrModify()
    {
        var handlers = typeof(InteractiveReprimandsModule)
            .GetMethods(Members)
            .Where(IsHandler)
            .Where(m => m.GetParameters().Any(p => p is { Name: "id" } && p.ParameterType == typeof(string)))
            .ToList();

        // pardon, update, view and delete, plus the pardon and update buttons and modals.
        Assert.Equal(8, handlers.Count);

        var unguarded = handlers
            .Where(m => !Guards(typeof(InteractiveReprimandsModule), m)
                .Any(g => g.All(p => Scopes(p).HasFlag(AuthorizationScope.History | AuthorizationScope.Modify))))
            .Select(m => m.ToString())
            .ToList();

        Assert.True(unguarded.Count == 0,
            $"Reprimand handlers without a History or Modify check:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unguarded));
    }

    [Fact]
    public void ReprimandAutocomplete_IsOnlyUsedOnGuardedCommands()
    {
        // The autocomplete runs the owning command's preconditions, so an unguarded command would reopen it.
        var methods = typeof(InteractiveReprimandsModule).Assembly.DefinedTypes
            .SelectMany(t => t.GetMethods(Members | BindingFlags.DeclaredOnly).Select(m => (Type: t, Method: m)))
            .Where(x => x.Method.GetParameters().Any(p
                => p.GetCustomAttribute<AutocompleteAttribute>()?.AutocompleteHandlerType == typeof(ReprimandAutocomplete)))
            .ToList();

        Assert.NotEmpty(methods);

        var unguarded = methods
            .Where(x => !Guards(x.Type, x.Method).Any())
            .Select(x => $"{x.Type.Name}.{x.Method.Name}")
            .ToList();

        Assert.True(unguarded.Count == 0,
            $"Commands using {nameof(ReprimandAutocomplete)} without an authorization precondition:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unguarded));
    }

    private static bool IsHandler(MethodInfo method)
        => method.IsDefined(typeof(SlashCommandAttribute))
            || method.IsDefined(typeof(ComponentInteractionAttribute))
            || method.IsDefined(typeof(ModalInteractionAttribute));

    /// <summary>
    ///     The authorization preconditions that guard a handler. Ungrouped preconditions must all pass, while a named
    ///     group passes when any one of its members passes, so a group only guards when it is made only of them.
    /// </summary>
    private static IEnumerable<IReadOnlyList<PreconditionAttribute>> Guards(Type module, MethodInfo method)
        => module.GetCustomAttributes<PreconditionAttribute>(true)
            .Concat(method.GetCustomAttributes<PreconditionAttribute>(true))
            .GroupBy(p => p.Group, StringComparer.Ordinal)
            .Where(g => g.Key is null ? g.Any(IsAuthorization) : g.All(IsAuthorization))
            .Select(g => (IReadOnlyList<PreconditionAttribute>) g.Where(IsAuthorization).ToList());

    private static bool IsAuthorization(PreconditionAttribute precondition)
        => precondition is RequireAuthorizationAttribute or RequireCategoryAuthorizationAttribute;

    private static AuthorizationScope Scopes(PreconditionAttribute precondition)
    {
        var field = precondition.GetType().GetField("_scopes", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return (AuthorizationScope) field.GetValue(precondition)!;
    }
}
