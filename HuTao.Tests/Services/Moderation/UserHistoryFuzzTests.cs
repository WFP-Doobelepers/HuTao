using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using FsCheck;
using FsCheck.Xunit;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Interactive.Paginator;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

public class UserHistoryFuzzTests
{
    [Property(MaxTest = 100)]
    public bool UserHistoryPaginator_WithRandomReprimandCounts_ProducesValidComponents(PositiveInt count)
    {
        var reprimandCount = Math.Min(count.Get, 200);
        var reprimands = GenerateRandomReprimands(reprimandCount, seed: count.Get);
        var state = CreateState(reprimands);

        return ValidateAllPages(state);
    }

    [Property(MaxTest = 50)]
    public bool UserHistoryPaginator_WithRandomReasonLengths_ProducesValidComponents(PositiveInt seed)
    {
        var rng = new Random(seed.Get);
        var reprimands = Enumerable.Range(0, rng.Next(5, 50))
            .Select(i => CreateReprimandWithReason(i, GenerateRandomReason(rng)))
            .ToList();

        var state = CreateState(reprimands);

        return ValidateAllPages(state);
    }

    [Property(MaxTest = 50)]
    public bool UserHistoryPaginator_WithMixedReprimandTypes_ProducesValidComponents(PositiveInt seed)
    {
        var rng = new Random(seed.Get);
        var count = rng.Next(10, 100);
        var reprimands = Enumerable.Range(0, count)
            .Select(i => CreateRandomReprimandType(i, rng))
            .ToList();

        var state = CreateState(reprimands);

        return ValidateAllPages(state);
    }

    [Property(MaxTest = 25)]
    public bool UserHistoryPaginator_WithExtremeReasonLengths_ProducesValidComponents(PositiveInt seed)
    {
        var rng = new Random(seed.Get);
        var reprimands = Enumerable.Range(0, rng.Next(5, 20))
            .Select(i =>
            {
                var reasonLength = rng.Next(1, 8000);
                var reason = new string('x', reasonLength);
                return CreateReprimandWithReason(i, reason);
            })
            .ToList();

        var state = CreateState(reprimands);

        return ValidateAllPages(state);
    }

    [Property(MaxTest = 25)]
    public bool UserHistoryPaginator_TotalComponentCount_NeverExceeds40(PositiveInt seed)
    {
        var rng = new Random(seed.Get);
        var count = rng.Next(1, 150);
        var reprimands = GenerateRandomReprimands(count, seed.Get);
        var state = CreateState(reprimands);

        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = GenerateUserHistoryPage(paginator, state);

            if (page.Components is null)
                continue;

            var componentCount = ComponentsV2Assertions.CountAllComponents(page.Components);
            if (componentCount > 40)
                return false;
        }

        return true;
    }

    private static bool ValidateAllPages(UserHistoryPaginatorState state)
    {
        var paginator = CreatePaginator(state);

        for (var pageIndex = 0; pageIndex < state.TotalPages; pageIndex++)
        {
            paginator.SetPage(pageIndex);
            var page = GenerateUserHistoryPage(paginator, state);

            if (page.Components is null)
                return false;

            try
            {
                page.Components.ShouldBeValidComponentsV2();
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static IPage GenerateUserHistoryPage(IComponentPaginator p, UserHistoryPaginatorState state)
    {
        var method = typeof(HuTao.Services.Moderation.UserService)
            .GetMethod("GenerateUserHistoryPage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method is null)
            throw new InvalidOperationException("GenerateUserHistoryPage method not found.");

        return (IPage)method.Invoke(null, new object[] { p, state })!;
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

    private static IReadOnlyList<Reprimand> GenerateRandomReprimands(int count, int seed)
    {
        var rng = new Random(seed);
        return Enumerable.Range(0, count)
            .Select(i => CreateRandomReprimandType(i, rng))
            .ToList();
    }

    private static Reprimand CreateRandomReprimandType(int index, Random rng)
    {
        var reasonLength = rng.Next(10, 500);
        var reason = GenerateRandomReason(rng, reasonLength);
        return CreateReprimandWithReason(index, reason, rng.Next(0, 5));
    }

    private static Reprimand CreateReprimandWithReason(int index, string reason, int typeIndex = 0)
    {
        var user = CreateMockUser((ulong)(1000 + index));
        var moderator = CreateMockGuildUser((ulong)(2000 + index));
        var details = new ReprimandDetails(user, moderator, reason);

        Reprimand reprimand = (typeIndex % 5) switch
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

    private static string GenerateRandomReason(Random rng, int? length = null)
    {
        var actualLength = length ?? rng.Next(10, 2000);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?";
        return new string(Enumerable.Range(0, actualLength)
            .Select(_ => chars[rng.Next(chars.Length)])
            .ToArray());
    }
}
