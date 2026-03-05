using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

public class ComponentLimitStressTests
{
    [Fact]
    public void UserHistoryPaginator_With100Reprimands_ProducesValidComponents()
    {
        var reprimands = CreateReprimands(100);
        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_With200Reprimands_ProducesValidComponents()
    {
        var reprimands = CreateReprimands(200);
        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_With500Reprimands_ProducesValidComponents()
    {
        var reprimands = CreateReprimands(500);
        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithMaxLengthReasons_ProducesValidComponents()
    {
        var reprimands = Enumerable.Range(0, 50)
            .Select(i => CreateReprimandWithReason(i, new string('x', 6000)))
            .ToList();

        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithMixedReasonLengths_ProducesValidComponents()
    {
        var rng = new Random(42);
        var reprimands = Enumerable.Range(0, 100)
            .Select(i =>
            {
                var reasonLength = rng.Next(1, 8000);
                return CreateReprimandWithReason(i, new string('r', reasonLength));
            })
            .ToList();

        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_AllPages_NeverExceed40Components()
    {
        var reprimands = CreateReprimands(300);
        var state = CreateState(reprimands);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = InvokeGenerateUserHistoryPage(paginator, state);

            Assert.NotNull(page.Components);

            var componentCount = ComponentsV2Assertions.CountAllComponents(page.Components);
            Assert.True(componentCount <= 40,
                $"Page {pageIndex} has {componentCount} components, exceeding the 40 component limit.");
        }
    }

    [Fact]
    public void UserHistoryPaginator_AllPages_NeverExceed4000CharsCumulativeText()
    {
        var reprimands = Enumerable.Range(0, 100)
            .Select(i => CreateReprimandWithReason(i, new string('t', 3000)))
            .ToList();

        var state = CreateState(reprimands);
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = InvokeGenerateUserHistoryPage(paginator, state);

            Assert.NotNull(page.Components);

            var validationResult = ComponentsV2Validator.Validate(page.Components);
            Assert.True(validationResult.IsValid,
                $"Page {pageIndex} validation failed: {validationResult}");
        }
    }

    [Fact]
    public void UserHistoryPaginator_WithEmptyReprimands_ProducesValidComponents()
    {
        var state = CreateState(Array.Empty<Reprimand>());

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithSingleReprimand_ProducesValidComponents()
    {
        var reprimands = CreateReprimands(1);
        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithAllReprimandTypes_ProducesValidComponents()
    {
        var reprimands = new List<Reprimand>();
        var user = CreateMockUser(123);
        var moderator = CreateMockGuildUser(456);

        reprimands.Add(new Warning(1, TimeSpan.FromDays(30), new ReprimandDetails(user, moderator, "Warning reason")));
        reprimands.Add(new Mute(TimeSpan.FromHours(1), new ReprimandDetails(user, moderator, "Mute reason")));
        reprimands.Add(new Note(new ReprimandDetails(user, moderator, "Note reason")));
        reprimands.Add(new Kick(new ReprimandDetails(user, moderator, "Kick reason")));
        reprimands.Add(new Ban(0, TimeSpan.FromDays(7), new ReprimandDetails(user, moderator, "Ban reason")));

        foreach (var r in reprimands)
            r.Id = Guid.NewGuid();

        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithUnicodeReasons_ProducesValidComponents()
    {
        var unicodeReasons = new[]
        {
            "日本語のテスト理由 🎮",
            "Тестовая причина на русском языке 🔥",
            "اختبار السبب بالعربية 💯",
            "ทดสอบเหตุผลภาษาไทย 🎉",
            "测试中文原因 ✨",
            string.Concat(Enumerable.Repeat("🔥", 100)),
            string.Concat(Enumerable.Repeat("émojis: 🎮🔥💯🎉✨ ", 50))
        };

        var reprimands = unicodeReasons
            .Select((reason, i) => CreateReprimandWithReason(i, reason))
            .ToList();

        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void UserHistoryPaginator_WithSpecialCharacterReasons_ProducesValidComponents()
    {
        var specialReasons = new[]
        {
            "Reason with `code blocks` and **bold** and __underline__",
            "Reason with [links](https://example.com) and ||spoilers||",
            "Reason with\nnewlines\nand\ttabs",
            "<script>alert('xss')</script>",
            "```csharp\nvar x = 1;\n```",
            new string('\n', 100),
            string.Concat(Enumerable.Repeat("line\n", 200))
        };

        var reprimands = specialReasons
            .Select((reason, i) => CreateReprimandWithReason(i, reason))
            .ToList();

        var state = CreateState(reprimands);

        AssertAllPagesValid(state);
    }

    [Fact]
    public void MuteListPaginator_With100Mutes_ProducesValidState()
    {
        var mutes = Enumerable.Range(0, 100)
            .Select(i =>
            {
                var user = CreateMockUser((ulong)(1000 + i));
                var moderator = CreateMockGuildUser((ulong)(2000 + i));
                var details = new ReprimandDetails(user, moderator, $"Mute reason {i}");
                var mute = new Mute(TimeSpan.FromHours(i + 1), details) { Id = Guid.NewGuid() };
                return mute;
            })
            .ToList();

        var guild = new GuildEntity(789);
        var state = new MuteListPaginatorState(mutes, null, guild);

        Assert.Equal(100, state.TotalMutes);
        Assert.True(state.TotalPages > 1);

        for (var i = 0; i < state.TotalPages; i++)
        {
            var pageMutes = state.GetMutesForPage(i).ToList();
            Assert.True(pageMutes.Count <= 3);
        }
    }

    private static void AssertAllPagesValid(UserHistoryPaginatorState state)
    {
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = InvokeGenerateUserHistoryPage(paginator, state);

            Assert.NotNull(page);
            Assert.NotNull(page.Components);
            page.Components.ShouldBeValidComponentsV2();
        }
    }

    private static IPage InvokeGenerateUserHistoryPage(IComponentPaginator paginator, UserHistoryPaginatorState state)
    {
        var method = typeof(HuTao.Services.Moderation.UserService)
            .GetMethod("GenerateUserHistoryPage", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var page = method!.Invoke(null, new object[] { paginator, state });
        Assert.NotNull(page);

        return (IPage)page!;
    }

    private static UserHistoryPaginatorState CreateState(IReadOnlyList<Reprimand> reprimands)
    {
        var user = CreateMockUser(123);
        var requestedBy = CreateMockUser(456);
        var userEntity = new GuildUserEntity(123, 789) { JoinedAt = DateTimeOffset.UtcNow.AddYears(-1) };
        var guild = new GuildEntity(789);

        return new UserHistoryPaginatorState(
            user, userEntity, reprimands,
            null, LogReprimandType.All, guild, requestedBy);
    }

    private static IComponentPaginator CreatePaginator(UserHistoryPaginatorState state)
    {
        var user = CreateMockUser(456);

        return InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(user)
            .WithUserState(state)
            .WithPageCount(state.TotalPages)
            .WithPageFactory(_ => throw new InvalidOperationException("Not used directly."))
            .Build();
    }

    private static IReadOnlyList<Reprimand> CreateReprimands(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateReprimandWithReason(i, $"Test reprimand reason {i}"))
            .ToList();
    }

    private static Reprimand CreateReprimandWithReason(int index, string reason)
    {
        var user = CreateMockUser((ulong)(1000 + index));
        var moderator = CreateMockGuildUser((ulong)(2000 + index));
        var details = new ReprimandDetails(user, moderator, reason);

        Reprimand reprimand = (index % 5) switch
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

    private static IGuildUser CreateMockGuildUser(ulong id)
    {
        var guildMock = new Mock<IGuild>();
        guildMock.SetupGet(g => g.Id).Returns(789);

        var mock = new Mock<IGuildUser>();
        mock.SetupGet(u => u.Id).Returns(id);
        mock.SetupGet(u => u.Guild).Returns(guildMock.Object);
        return mock.Object;
    }
}
