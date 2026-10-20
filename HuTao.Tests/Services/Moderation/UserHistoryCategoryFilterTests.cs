using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using HuTao.Tests.Testing;
using Moq;
using Xunit;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Tests.Services.Moderation;

/// <summary>
///     The history paginator's category filter must only offer the categories the requester may view. The filter's
///     handler refuses any other choice with the same list, so a moderator scoped to one category cannot read the
///     reprimands of the others.
/// </summary>
public class UserHistoryCategoryFilterTests
{
    [Fact]
    public async Task CategoryModerator_GetsOnlyTheirCategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, moderator);

        var categories = await HistoryCategoriesAsync(fixture, moderator);

        Assert.Equal(new[] { appeals }, categories);
    }

    [Fact]
    public async Task GuildModerator_GetsAllAndEveryCategory()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var reports = fixture.AddCategory("Reports");
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        var categories = await HistoryCategoriesAsync(fixture, moderator);

        Assert.Equal(new[] { ModerationCategory.All, appeals, reports }, categories);
    }

    [Fact]
    public async Task Member_GetsNoCategories()
    {
        var fixture = new AuthorizationFixture();
        var appeals = fixture.AddCategory("Appeals");
        var member = fixture.User(200, defaultCategory: appeals);
        AuthorizationFixture.Allow(appeals.Authorization, AuthorizationScope.History, fixture.User(100));

        Assert.Empty(await HistoryCategoriesAsync(fixture, member));
    }

    [Fact]
    public async Task GuildWithoutCategories_HasNoFilter()
    {
        var fixture = new AuthorizationFixture();
        var moderator = fixture.User(100);
        AuthorizationFixture.Allow(fixture.Guild.AuthorizationGroups, AuthorizationScope.History, moderator);

        Assert.Empty(await HistoryCategoriesAsync(fixture, moderator));
    }

    [Fact]
    public void Page_OffersOnlyTheFilterCategories()
    {
        var guild = new GuildEntity(1);
        var appeals = Category(guild, "Appeals");
        Category(guild, "Reports");

        var filter = CategoryFilter(State(guild, [appeals]));

        Assert.NotNull(filter);
        Assert.Equal(new[] { appeals.Id.ToString() }, filter.Options.Select(o => o.Value));
    }

    [Fact]
    public void Page_WithoutFilterCategories_HasNoFilter()
    {
        var guild = new GuildEntity(1);
        Category(guild, "Appeals");

        Assert.Null(CategoryFilter(State(guild, [])));
    }

    private static Task<IReadOnlyList<ModerationCategory>> HistoryCategoriesAsync(
        AuthorizationFixture fixture, IGuildUser user)
        => new UserService(fixture.Auth, null!, null!, fixture.Db)
            .GetHistoryCategoriesAsync(new InteractionContext(fixture.Interaction(user)), fixture.Guild);

    private static ModerationCategory Category(GuildEntity guild, string name)
    {
        var category = new ModerationCategory(name, null, null) { Id = Guid.NewGuid() };
        guild.ModerationCategories.Add(category);
        return category;
    }

    private static UserHistoryPaginatorState State(GuildEntity guild, IReadOnlyList<ModerationCategory> categories)
    {
        var user = User(42);
        var moderator = new Mock<IGuildUser>();
        moderator.SetupGet(m => m.Id).Returns(2);
        moderator.SetupGet(m => m.Guild).Returns(Mock.Of<IGuild>(g => g.Id == guild.Id));

        var reprimands = new List<Reprimand> { new Note(new ReprimandDetails(user, moderator.Object, "reason")) };

        return new UserHistoryPaginatorState(
            user, new GuildUserEntity(42, guild.Id), reprimands,
            null, LogReprimandType.All, guild, User(999))
        {
            FilterCategories = categories
        };
    }

    private static SelectMenuComponent? CategoryFilter(UserHistoryPaginatorState state)
    {
        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(state.RequestedBy)
            .WithUserState(state)
            .WithPageCount(state.TotalPages)
            .WithPageFactory(_ => throw new InvalidOperationException("Not used in test."))
            .Build();

        var method = typeof(UserService)
            .GetMethod("GenerateUserHistoryPage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var page = (IPage) method.Invoke(null, [paginator, state])!;
        Assert.NotNull(page.Components);

        return page.Components.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<SelectMenuComponent>()
            .SingleOrDefault(s => s.CustomId == "history-category-filter");
    }

    private static IUser User(ulong id)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Mention).Returns($"<@{id}>");
        user.SetupGet(u => u.CreatedAt).Returns(DateTimeOffset.UtcNow.AddYears(-2));
        user.Setup(u => u.GetDisplayAvatarUrl(It.IsAny<ImageFormat>(), It.IsAny<ushort>()))
            .Returns($"https://cdn.discordapp.com/avatars/{id}/avatar.png");
        user.Setup(u => u.GetDefaultAvatarUrl()).Returns("https://cdn.discordapp.com/embed/avatars/0.png");
        return user.Object;
    }
}
