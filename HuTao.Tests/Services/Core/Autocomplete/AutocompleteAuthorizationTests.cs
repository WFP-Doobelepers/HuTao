using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Bot.Modules;
using HuTao.Services.Core.Autocomplete;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core.Autocomplete;

/// <summary>
///     Discord.Net does not run a command's preconditions before its autocomplete, so every handler must run them
///     itself, or anyone could read suggestions for commands they may not use.
/// </summary>
public class AutocompleteAuthorizationTests
{
    private const BindingFlags Members
        = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly;

    private static readonly Assembly Bot = typeof(HelpModule).Assembly;

    [Fact]
    public async Task EveryAutocomplete_RefusesWhenPreconditionsFail()
    {
        var handlers = HandlerTypes().ToList();
        Assert.True(handlers.Count >= 9, $"Only {handlers.Count} autocomplete handlers were found.");

        var failures = new List<string>();
        foreach (var type in handlers)
        {
            var handler = (AutocompleteHandler) Activator.CreateInstance(type)!;

            // Strict mocks throw on any use, so a handler that skips the check fails here.
            try
            {
                var result = await handler.GenerateSuggestionsAsync(
                    new Mock<IInteractionContext>(MockBehavior.Strict).Object,
                    new Mock<IAutocompleteInteraction>(MockBehavior.Strict).Object,
                    Parameter(PreconditionResult.FromError("denied")),
                    new Mock<IServiceProvider>(MockBehavior.Strict).Object);

                if (!result.IsSuccess || result.Suggestions is not { Count: 0 })
                    failures.Add($"{type.Name} returned suggestions.");
            }
            catch (Exception e)
            {
                failures.Add($"{type.Name} did not stop before using {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(failures.Count == 0,
            $"Autocomplete handlers that answer when the command's preconditions fail:{Environment.NewLine}"
            + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void EveryAutocompleteAttribute_NamesAnAuthorizedHandler()
    {
        // A handler-less [Autocomplete] routes to an [AutocompleteCommand], which runs only its own preconditions.
        var attributes = AutocompleteAttributes().ToList();
        Assert.NotEmpty(attributes);

        var unauthorized = attributes
            .Where(a => a.Attribute.AutocompleteHandlerType is not { } type
                || !typeof(AuthorizedAutocompleteHandler).IsAssignableFrom(type))
            .Select(a => $"{a.Location} uses {a.Attribute.AutocompleteHandlerType?.Name ?? "an autocomplete command"}")
            .ToList();

        Assert.True(unauthorized.Count == 0,
            $"Autocompletes that do not run the command's preconditions:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unauthorized));
    }

    [Fact]
    public async Task PassedPreconditions_CallTheHandler()
    {
        var handler = new RecordingAutocomplete();

        var result = await handler.GenerateSuggestionsAsync(
            Mock.Of<IInteractionContext>(), Mock.Of<IAutocompleteInteraction>(),
            Parameter(PreconditionResult.FromSuccess()), Mock.Of<IServiceProvider>());

        Assert.True(handler.Called);
        Assert.Single(result.Suggestions);
    }

    private static IEnumerable<Type> HandlerTypes()
    {
        var defined = new[] { typeof(AuthorizedAutocompleteHandler).Assembly, Bot }
            .SelectMany(a => a.DefinedTypes)
            .Where(t => typeof(AutocompleteHandler).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => t.AsType());

        var attached = AutocompleteAttributes()
            .Select(a => a.Attribute.AutocompleteHandlerType)
            .OfType<Type>();

        return defined.Concat(attached).Distinct();
    }

    private static IEnumerable<(string Location, AutocompleteAttribute Attribute)> AutocompleteAttributes()
        => Bot.DefinedTypes
            .SelectMany(t => t.GetMethods(Members).Cast<MethodBase>().Concat(t.GetConstructors(Members)))
            .SelectMany(m => m.GetParameters()
                .Select(p => (Location: $"{m.DeclaringType?.Name}.{m.Name}({p.Name})",
                    Attribute: p.GetCustomAttribute<AutocompleteAttribute>()!)))
            .Where(x => x.Attribute is not null);

    private static IParameterInfo Parameter(PreconditionResult preconditions)
    {
        var command = new Mock<ICommandInfo>();
        command
            .Setup(c => c.CheckPreconditionsAsync(It.IsAny<IInteractionContext>(), It.IsAny<IServiceProvider>()))
            .ReturnsAsync(preconditions);

        var parameter = new Mock<IParameterInfo>();
        parameter.SetupGet(p => p.Command).Returns(command.Object);
        return parameter.Object;
    }

    private class RecordingAutocomplete : AuthorizedAutocompleteHandler
    {
        public bool Called { get; private set; }

        protected override Task<AutocompletionResult> GetSuggestionsAsync(
            IInteractionContext context, IAutocompleteInteraction interaction,
            IParameterInfo parameter, IServiceProvider services)
        {
            Called = true;
            return Task.FromResult(AutocompletionResult.FromSuccess([new AutocompleteResult("name", "value")]));
        }
    }
}
