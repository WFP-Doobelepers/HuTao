using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.Interactions;
using HuTao.Bot.Modules;
using Xunit;

namespace HuTao.Tests.Bot;

/// <summary>
///     The autocompletes and the authorization checks read the guild, which a direct message does not have, so a module
///     that offers an autocomplete must refuse to run outside a guild instead of throwing.
/// </summary>
public class InteractionModuleContextTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void ModulesWithAnAutocomplete_RequireAGuild()
    {
        var modules = typeof(HelpModule).Assembly.DefinedTypes
            .Where(t => typeof(IInteractionModuleBase).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetMethods(Members)
                .Any(m => m.GetParameters().Any(p => p.IsDefined(typeof(AutocompleteAttribute)))))
            .ToList();

        Assert.True(modules.Count >= 16, $"Only {modules.Count} modules with an autocomplete were found.");

        var open = modules
            .Where(t => !Enclosing(t).Any(RequiresGuild))
            .Select(t => t.Name)
            .Order()
            .ToList();

        Assert.True(open.Count == 0,
            $"Modules with an autocomplete that also run in direct messages:{Environment.NewLine}"
            + string.Join(Environment.NewLine, open));
    }

    private static bool RequiresGuild(Type module)
        => module.GetCustomAttributes<RequireContextAttribute>(true).Any(a => a.Contexts == ContextType.Guild);

    private static IEnumerable<Type> Enclosing(Type module)
    {
        for (var type = module; type is not null; type = type.DeclaringType)
            yield return type;
    }
}
