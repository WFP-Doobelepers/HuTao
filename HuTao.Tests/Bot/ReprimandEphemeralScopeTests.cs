using System.Linq;
using System.Reflection;
using Discord.Interactions;
using HuTao.Bot.Modules.Moderation;
using HuTao.Services.Core.Preconditions.Interactions;
using Xunit;

namespace HuTao.Tests.Bot;

// A reprimand's buttons carry the hidden-reply flag of the command that showed them, and a member can build a button's
// custom ID by hand. Every handler of the reprimand commands that reads the flag checks the Ephemeral scope, as the
// slash commands that act on a reprimand do.
public class ReprimandEphemeralScopeTests
{
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly;

    [Fact]
    public void EveryHiddenReplyFlag_AsksForTheEphemeralScope()
    {
        var flags = typeof(InteractiveReprimandsModule).GetMethods(Methods)
            .Where(m => m.IsDefined(typeof(SlashCommandAttribute))
                || m.IsDefined(typeof(ComponentInteractionAttribute))
                || m.IsDefined(typeof(ModalInteractionAttribute)))
            .SelectMany(m => m.GetParameters()
                .Where(p => p.ParameterType == typeof(bool) && p.Name == "ephemeral")
                .Select(p => (Method: m, Parameter: p)))
            .ToList();

        // Slash pardon, update, history, view and delete, the pardon, update and history buttons, and the two forms.
        Assert.True(flags.Count >= 10, $"Only {flags.Count} hidden-reply flags were found.");
        Assert.Empty(flags
            .Where(f => !f.Parameter.IsDefined(typeof(RequireEphemeralScopeAttribute)))
            .Select(f => $"{f.Method.Name}({string.Join(", ", f.Method.GetParameters().Select(p => p.Name))})"));
    }
}
