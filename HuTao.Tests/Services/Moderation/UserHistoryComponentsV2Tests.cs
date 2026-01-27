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
using HuTao.Services.Utilities;
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateUserHistoryPage_WithVaryingReprimandCounts_ProducesValidComponents(int count)
    {
        var state = CreateStateWithReprimands(count);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            var components = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex);
            components.ShouldBeValidComponentsV2();
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(3000)]
    [InlineData(5000)]
    public void GenerateUserHistoryPage_WithVaryingReasonLengths_ProducesValidComponents(int reasonLength)
    {
        var reprimands = Enumerable.Range(0, 10)
            .Select(i => CreateReprimandWithReason(i, new string('x', reasonLength)))
            .ToList();

        var state = CreateStateWithReprimandList(reprimands);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            var components = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex);
            components.ShouldBeValidComponentsV2();
        }
    }

    [Theory]
    [InlineData(LogReprimandType.Warning)]
    [InlineData(LogReprimandType.Mute)]
    [InlineData(LogReprimandType.Note)]
    [InlineData(LogReprimandType.Kick)]
    [InlineData(LogReprimandType.Ban)]
    [InlineData(LogReprimandType.All)]
    public void GenerateUserHistoryPage_WithDifferentFilters_ProducesValidComponents(LogReprimandType filter)
    {
        var state = CreateStateWithReprimands(20);
        state.UpdateFilters(null, filter);

        var paginator = CreatePaginator(state);
        paginator.PageCount = state.TotalPages;

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            var components = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex);
            components.ShouldBeValidComponentsV2();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GenerateUserHistoryPage_ComponentCount_NeverExceeds40(int reprimandsPerType)
    {
        var reprimands = new List<Reprimand>();
        for (var typeIndex = 0; typeIndex < 5; typeIndex++)
        {
            for (var i = 0; i < reprimandsPerType; i++)
            {
                reprimands.Add(CreateReprimandWithReason(i + typeIndex * 100, $"Reason {i}", typeIndex));
            }
        }

        var state = CreateStateWithReprimandList(reprimands);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            var components = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex);

            var componentCount = ComponentsV2Assertions.CountAllComponents(components);
            Assert.True(componentCount <= 40,
                $"Page {pageIndex} has {componentCount} components, exceeding the 40 limit.");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GenerateUserHistoryPage_CumulativeTextLength_NeverExceeds4000(int reprimandCount)
    {
        var reprimands = Enumerable.Range(0, reprimandCount)
            .Select(i => CreateReprimandWithReason(i, new string('x', 1000)))
            .ToList();

        var state = CreateStateWithReprimandList(reprimands);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            var components = InvokeGenerateUserHistoryComponents(paginator, state, pageIndex);

            var result = ComponentsV2Validator.Validate(components);
            Assert.True(result.IsValid, $"Page {pageIndex} validation failed: {result}");
        }
    }

    private static UserHistoryPaginatorState CreateStateWithReprimands(int count)
    {
        var reprimands = Enumerable.Range(0, count)
            .Select(i => CreateReprimandWithReason(i, $"Test reason {i}"))
            .ToList();

        return CreateStateWithReprimandList(reprimands);
    }

    private static UserHistoryPaginatorState CreateStateWithReprimandList(IReadOnlyList<Reprimand> reprimands)
    {
        var user = CreateMockUser(42);
        var requestedBy = CreateMockUser(999);
        var userEntity = new GuildUserEntity(42, 123456789) { JoinedAt = DateTimeOffset.UtcNow.AddYears(-1) };
        var guild = new GuildEntity(123456789);
        var category = new ModerationCategory("Default", null, null) { Id = Guid.NewGuid() };
        guild.ModerationCategories.Add(category);

        return new UserHistoryPaginatorState(
            user, userEntity, reprimands,
            null, LogReprimandType.All, guild, requestedBy, new byte[] { 1, 2, 3 });
    }

    private static IComponentPaginator CreatePaginator(UserHistoryPaginatorState state)
    {
        var requestedBy = CreateMockUser(999);

        return InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(requestedBy)
            .WithUserState(state)
            .WithPageCount(state.TotalPages)
            .WithPageFactory(_ => throw new InvalidOperationException("Not used in test."))
            .Build();
    }

    private static Reprimand CreateReprimandWithReason(int index, string reason, int typeIndex = -1)
    {
        var user = CreateMockUser((ulong)(1000 + index));
        var moderator = CreateMockGuildUser((ulong)(2000 + index));
        var details = new ReprimandDetails(user, moderator, reason);

        var actualTypeIndex = typeIndex >= 0 ? typeIndex : index;

        Reprimand reprimand = (actualTypeIndex % 5) switch
        {
            0 => new Warning((uint)(index + 1), TimeSpan.FromDays(30), details),
            1 => new Mute(TimeSpan.FromHours(index + 1), details),
            2 => new Note(details),
            3 => new Kick(details),
            _ => new Ban(0, TimeSpan.FromDays(7), details)
        };

        reprimand.Id = Guid.NewGuid();
        return reprimand;
    }

    private static IGuildUser CreateMockGuildUser(ulong id)
    {
        var guildMock = new Mock<IGuild>();
        guildMock.SetupGet(g => g.Id).Returns(123456789);

        var mock = new Mock<IGuildUser>();
        mock.SetupGet(u => u.Id).Returns(id);
        mock.SetupGet(u => u.Guild).Returns(guildMock.Object);
        return mock.Object;
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

