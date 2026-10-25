using System.Threading.Tasks;
using Discord;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Xunit;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Tests.Services.Moderation;

// A template's reprimand goes in the template's category, or in the moderator's default category when the template has
// none. Applying it needs the template's scope in that category, the same check the moderation command runs there.
public class TemplateAccessTests
{
    private const ulong ModeratorId = 100;

    [Fact]
    public async Task AServerWideModerator_IsRefusedATemplateOfACategoryWithNoRuleForThem()
    {
        var fixture = new AuthorizationFixture();
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Ban, moderator);

        Assert.False(await MayApplyAsync(fixture, moderator, Template(spam)));
    }

    [Fact]
    public async Task AModeratorWithTheScopeInTheTemplatesCategory_MayApplyIt()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, moderator);

        Assert.True(await MayApplyAsync(fixture, moderator, Template(raids)));
    }

    [Fact]
    public async Task AModeratorWithTheScopeInAnotherCategory_IsRefused()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var spam = fixture.AddCategory("Spam");
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(raids.Authorization, AuthorizationScope.Ban, moderator);

        Assert.False(await MayApplyAsync(fixture, moderator, Template(spam)));
    }

    [Fact]
    public async Task ATemplateWithNoCategory_IsCheckedInTheModeratorsDefaultCategory()
    {
        var fixture = new AuthorizationFixture();
        var raids = fixture.AddCategory("Raids");
        var moderator = fixture.User(ModeratorId, raids);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Ban, moderator);

        Assert.False(await MayApplyAsync(fixture, moderator, Template(null)));
    }

    [Fact]
    public async Task ATemplateWithNoCategory_AndNoDefaultCategory_IsCheckedInTheServer()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(ModeratorId);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.Ban, moderator);

        Assert.True(await MayApplyAsync(fixture, moderator, Template(null)));
    }

    private static ModerationTemplate Template(ModerationCategory? category)
        => new("spam", new BanAction(0, null), new ModerationTemplatesModule.BanTemplateOptions()) { Category = category };

    private static Task<bool> MayApplyAsync(AuthorizationFixture fixture, IGuildUser moderator, ModerationTemplate template)
        => fixture.Auth.MayApplyAsync(new CommandContext(fixture.Command(moderator)), template);
}
