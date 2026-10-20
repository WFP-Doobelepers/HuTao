using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Services.Core.Autocomplete;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core.Autocomplete;

public class ReprimandAutocompleteTests
{
    [Fact]
    public async Task FailedPreconditions_ReturnNoSuggestions()
    {
        // Strict mocks throw on any use, which proves nothing is looked up for a refused user.
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        var context = new Mock<IInteractionContext>(MockBehavior.Strict);
        var interaction = new Mock<IAutocompleteInteraction>(MockBehavior.Strict);

        var result = await new ReprimandAutocomplete().GenerateSuggestionsAsync(
            context.Object, interaction.Object,
            Parameter(PreconditionResult.FromError("denied")), services.Object);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task PassedPreconditions_LookUpReprimands()
    {
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(HuTaoContext))).Returns(null!);

        // The lookup starts by resolving the database, which this provider does not have.
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReprimandAutocomplete()
            .GenerateSuggestionsAsync(
                Mock.Of<IInteractionContext>(), Mock.Of<IAutocompleteInteraction>(),
                Parameter(PreconditionResult.FromSuccess()), services.Object));

        services.Verify(s => s.GetService(typeof(HuTaoContext)), Times.Once);
    }

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
}
