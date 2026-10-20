using System.Linq;
using System.Threading.Tasks;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Autocomplete;
using HuTao.Tests.Testing;
using Xunit;

namespace HuTao.Tests.Services.Core.Autocomplete;

public class CategoryAutocompleteTests
{
    [Fact]
    public async Task SuggestsOnlyTheCategoriesTheParameterAllows()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        fixture.AddCategory("Reports");
        var moderator = fixture.User(100);

        // A [CheckCategory] that only allows Appeals, so None and All must be hidden as well.
        var result = await new CategoryAutocomplete().GenerateSuggestionsAsync(
            fixture.Interaction(moderator), AutocompleteTesting.Interaction(string.Empty),
            AutocompleteTesting.Parameter(value => ReferenceEquals(value, appeals)), fixture.Services);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Appeals" }, result.Suggestions.Select(s => s.Value));
    }

    [Fact]
    public async Task SuggestsEveryCategory_WhenTheParameterHasNoCheck()
    {
        var fixture = new AuthorizationFixture();
        fixture.AddCategory("Appeals");
        fixture.AddCategory("Reports");

        var result = await new CategoryAutocomplete().GenerateSuggestionsAsync(
            fixture.Interaction(fixture.User(100)), AutocompleteTesting.Interaction(string.Empty),
            AutocompleteTesting.Parameter(), fixture.Services);

        Assert.Equal(
            new[] { ModerationCategory.None.Name, "Appeals", "Reports", ModerationCategory.All.Name },
            result.Suggestions.Select(s => s.Value));
    }
}
