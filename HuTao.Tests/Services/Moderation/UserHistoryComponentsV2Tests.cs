using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord;
using Fergun.Interactive.Pagination;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

public class UserHistoryComponentsV2Tests
{
    [Fact]
    public void GenerateUserHistoryPage_ProducesValidComponentsV2_ForAllDisplayModes()
    {
        var guildId = 123456789UL;
        var userId = 42UL;

        var user = CreateMockUser(userId);
        var requestedBy = CreateMockUser(999UL);

        var userEntity = new GuildUserEntity(userId, guildId)
        {
            JoinedAt = DateTimeOffset.UtcNow.AddYears(-1)
        };

        var guild = new GuildEntity(guildId);
        var category = new ModerationCategory("Default", null, null) { Id = Guid.NewGuid() };
        guild.ModerationCategories.Add(category);

        var reprimands = CreateLongReasonReprimands(count: 12, userId: userId, guildId: guildId);
        var imageBytes = new byte[] { 1, 2, 3 };

        var state = new UserHistoryPaginatorState(
            user,
            userEntity,
            reprimands,
            category: null,
            LogReprimandType.All,
            guild,
            requestedBy,
            imageBytes);

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(requestedBy)
            .WithUserState(state)
            .WithPageCount(state.TotalPages)
            .WithPageFactory(_ => throw new InvalidOperationException("Not used in test."))
            .Build();

        // Validate grouped view (only mode)
        state.UpdateFilters(category: null, type: LogReprimandType.All);
        paginator.PageCount = state.TotalPages;

        var components0 = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex: 0);
        components0.ShouldBeValidComponentsV2();

        if (state.TotalPages > 1)
        {
            var components1 = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex: 1);
            components1.ShouldBeValidComponentsV2();
        }
    }

    private static MessageComponent InvokeGenerateUserHistoryComponents(
        IComponentPaginator paginator,
        UserHistoryPaginatorState state,
        int pageIndex)
    {
        paginator.SetPage(pageIndex);

        var method = typeof(UserService)
            .GetMethod("GenerateUserHistoryPage", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var page = method!.Invoke(null, new object[] { paginator, state });
        Assert.NotNull(page);

        var componentsProperty = page!.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(componentsProperty);

        var components = (MessageComponent?)componentsProperty!.GetValue(page);
        Assert.NotNull(components);

        return components!;
    }

    private static IUser CreateMockUser(ulong id)
    {
        var mock = new Mock<IUser>();
        mock.SetupGet(u => u.Id).Returns(id);
        mock.SetupGet(u => u.Mention).Returns($"<@{id}>");
        mock.SetupGet(u => u.CreatedAt).Returns(DateTimeOffset.UtcNow.AddYears(-2));
        mock.Setup(u => u.GetDisplayAvatarUrl(It.IsAny<ImageFormat>(), It.IsAny<ushort>()))
            .Returns($"https://cdn.discordapp.com/avatars/{id}/avatar.png");
        mock.Setup(u => u.GetDefaultAvatarUrl()).Returns("https://cdn.discordapp.com/embed/avatars/0.png");
        return mock.Object;
    }

    private static IReadOnlyList<Reprimand> CreateLongReasonReprimands(int count, ulong userId, ulong guildId)
    {
        var guildMock = new Mock<IGuild>();
        guildMock.SetupGet(g => g.Id).Returns(guildId);

        var moderator = new Mock<IGuildUser>();
        moderator.SetupGet(u => u.Id).Returns(777UL);
        moderator.SetupGet(u => u.Guild).Returns(guildMock.Object);

        var user = CreateMockUser(userId);

        var list = new List<Reprimand>();
        for (var i = 0; i < count; i++)
        {
            var reason = $"{i}: {new string('r', 6000)}";
            var details = new ReprimandDetails(user, moderator.Object, reason);

            Reprimand reprimand = (i % 3) switch
            {
                0 => new Warning(1, TimeSpan.FromDays(30), details),
                1 => new Mute(TimeSpan.FromHours(1), details),
                _ => new Note(details)
            };

            reprimand.Id = Guid.NewGuid();
            list.Add(reprimand);
        }

        return list;
    }
}

