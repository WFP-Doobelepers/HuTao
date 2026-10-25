using System.Threading.Tasks;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core.Autocomplete;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Core.Autocomplete;

public class TemplateAutocompleteTests
{
    [Fact]
    public async Task Suggestions_StopAtDiscordsLimit()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Note, moderator);

        var options = Mock.Of<ITemplateOptions>(o => o.Scope == AuthorizationScope.Note && o.Reason == "reason");
        for (var i = 0; i < 30; i++)
            fixture.Guild.ModerationTemplates.Add(new ModerationTemplate($"note-{i}", new NoteAction(), options));

        var result = await new TemplateAutocomplete().GenerateSuggestionsAsync(
            fixture.Interaction(moderator), AutocompleteTesting.Interaction(string.Empty),
            AutocompleteTesting.Parameter(), fixture.Services);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Suggestions.Count);
    }

    [Fact]
    public async Task Suggestions_ListOnlyTheTemplatesOfTheCategoryWhereTheModeratorHasTheScope()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Note, moderator);

        var options = Mock.Of<ITemplateOptions>(o => o.Scope == AuthorizationScope.Note && o.Reason == "reason");
        fixture.Guild.ModerationTemplates.Add(new ModerationTemplate("raid-note", new NoteAction(), options) { Category = raids });
        fixture.Guild.ModerationTemplates.Add(new ModerationTemplate("spam-note", new NoteAction(), options) { Category = spam });

        var result = await new TemplateAutocomplete().GenerateSuggestionsAsync(
            fixture.Interaction(moderator), AutocompleteTesting.Interaction(string.Empty),
            AutocompleteTesting.Parameter(), fixture.Services);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.StartsWith("raid-note:", suggestion.Name);
    }
}
