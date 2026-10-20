using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core.Autocomplete;
using HuTao.Tests.Testing;
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

    [Fact]
    public async Task CategoryModerator_SeesOnlyTheirCategorysReprimands()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        var inAppeals = AddReprimand(fixture, appeals);
        AddReprimand(fixture, reports);
        AddReprimand(fixture, null);

        Assert.Equal(new[] { inAppeals.Id.ToString() }, await SuggestAsync(fixture, moderator));
    }

    [Fact]
    public async Task GuildModerator_SeesReprimandsWithoutACategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        AddReprimand(fixture, appeals);
        var uncategorized = AddReprimand(fixture, null);

        Assert.Equal(new[] { uncategorized.Id.ToString() }, await SuggestAsync(fixture, moderator));
    }

    [Fact]
    public async Task Suggestions_FillUpWithVisibleReprimands()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        // Hidden reprimands first, so a filter applied after the cap of 25 would leave nothing to suggest.
        for (var i = 0; i < 30; i++) AddReprimand(fixture, reports, minutesAgo: 30 - i);
        for (var i = 0; i < 30; i++) AddReprimand(fixture, appeals, minutesAgo: 100 + i);

        Assert.Equal(25, (await SuggestAsync(fixture, moderator)).Length);
    }

    private static async Task<object[]> SuggestAsync(AuthorizationFixture fixture, IGuildUser user)
    {
        var result = await new ReprimandAutocomplete().GenerateSuggestionsAsync(
            fixture.Interaction(user), AutocompleteTesting.Interaction(string.Empty),
            AutocompleteTesting.Parameter(), fixture.Services);

        Assert.True(result.IsSuccess);
        return result.Suggestions.Select(s => s.Value).ToArray();
    }

    private static Reprimand AddReprimand(AuthorizationFixture fixture, ModerationCategory? category, int minutesAgo = 0)
    {
        var reprimand = new Note(new ReprimandDetails(fixture.User(400), fixture.User(300), "reason"))
        {
            Id = Guid.NewGuid(),
            Category = category
        };

        reprimand.Action!.Date = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo);
        fixture.Guild.ReprimandHistory.Add(reprimand);
        return reprimand;
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
