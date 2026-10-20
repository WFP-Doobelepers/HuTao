using System;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Moq;

namespace HuTao.Tests.Testing;

public static class AutocompleteTesting
{
    /// <summary>An autocomplete request where the user has typed <paramref name="input" />.</summary>
    public static IAutocompleteInteraction Interaction(string input)
    {
        // Discord.Net only builds options from gateway payloads, so its constructor is internal.
        var option = (AutocompleteOption) Activator.CreateInstance(
            typeof(AutocompleteOption), BindingFlags.Instance | BindingFlags.NonPublic, null,
            [ApplicationCommandOptionType.String, "input", input, true], null)!;

        var data = new Mock<IAutocompleteInteractionData>();
        data.SetupGet(d => d.Current).Returns(option);

        var interaction = new Mock<IAutocompleteInteraction>();
        interaction.SetupGet(i => i.Data).Returns(data.Object);
        return interaction.Object;
    }

    /// <summary>
    ///     A parameter whose command's preconditions pass, and whose own preconditions pass only for the values
    ///     <paramref name="allowed" /> accepts.
    /// </summary>
    public static IParameterInfo Parameter(Func<object, bool>? allowed = null)
    {
        var command = new Mock<ICommandInfo>();
        command
            .Setup(c => c.CheckPreconditionsAsync(It.IsAny<IInteractionContext>(), It.IsAny<IServiceProvider>()))
            .ReturnsAsync(PreconditionResult.FromSuccess());

        var parameter = new Mock<IParameterInfo>();
        parameter.SetupGet(p => p.Command).Returns(command.Object);
        parameter
            .Setup(p => p.CheckPreconditionsAsync(
                It.IsAny<IInteractionContext>(), It.IsAny<object>(), It.IsAny<IServiceProvider>()))
            .ReturnsAsync((IInteractionContext _, object value, IServiceProvider _)
                => allowed is null || allowed(value)
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("denied"));

        return parameter.Object;
    }
}
