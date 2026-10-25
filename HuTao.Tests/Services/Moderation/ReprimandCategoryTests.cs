using System.Threading.Tasks;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

// With no category given, a moderation command's category check passes on the moderator's default category. The
// reprimand goes in that same category, so a moderator who holds a scope in one category cannot use it to reprimand in
// the server or to lift a mute of another category.
public class ReprimandCategoryTests
{
    [Fact]
    public async Task NoCategoryGiven_UsesTheModeratorsDefaultCategory()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var moderator = fixture.User(100, raids);

        Assert.Same(raids, await fixture.Db.Users.ForModeratorAsync(moderator, null));
    }

    [Fact]
    public async Task NoCategoryGiven_AndNoDefault_IsTheServer()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(100);

        Assert.Null(await fixture.Db.Users.ForModeratorAsync(moderator, null));
    }

    [Fact]
    public async Task AGivenCategory_IsKept()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(100, raids);

        Assert.Same(spam, await fixture.Db.Users.ForModeratorAsync(moderator, spam));
    }
}
