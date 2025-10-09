using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Interactive.Paginator;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

public class ComponentPaginatorTests
{
    #region MuteListPaginatorState Tests

    [Fact]
    public void MuteListPaginatorState_CalculatesTotalPages_Correctly()
    {
        // Arrange
        var mutes = CreateTestMutes(7); // 7 mutes, 3 per page = 3 pages
        var guild = CreateTestGuild();

        // Act
        var state = new MuteListPaginatorState(mutes, null, guild);

        // Assert
        Assert.Equal(7, state.TotalMutes);
        Assert.Equal(3, state.TotalPages); // Math.Ceiling(7/3) = 3
    }

    [Fact]
    public void MuteListPaginatorState_GetMutesForPage_ReturnsCorrectMutes()
    {
        // Arrange
        var mutes = CreateTestMutes(7);
        var guild = CreateTestGuild();
        var state = new MuteListPaginatorState(mutes, null, guild);

        // Act
        var page0 = state.GetMutesForPage(0).ToList();
        var page1 = state.GetMutesForPage(1).ToList();
        var page2 = state.GetMutesForPage(2).ToList();

        // Assert
        Assert.Equal(3, page0.Count);
        Assert.Equal(3, page1.Count);
        Assert.Single(page2); // Last page has only 1 mute
    }

    [Fact]
    public void MuteListPaginatorState_UpdateData_RecalculatesPages()
    {
        // Arrange
        var initialMutes = CreateTestMutes(7);
        var guild = CreateTestGuild();
        var state = new MuteListPaginatorState(initialMutes, null, guild);

        // Act
        var newMutes = CreateTestMutes(4);
        state.UpdateData(newMutes, null);

        // Assert
        Assert.Equal(4, state.TotalMutes);
        Assert.Equal(2, state.TotalPages); // Math.Ceiling(4/3) = 2
    }

    [Fact]
    public void MuteListPaginatorState_GetMuteDisplayInfo_FormatsCorrectly()
    {
        // Arrange
        var mute = new Mute(TimeSpan.FromHours(24), new ReprimandDetails(
            CreateMockUser(123), CreateMockGuildUser(456), "Test reason"));
        var guild = CreateTestGuild();
        var state = new MuteListPaginatorState(new[] { mute }, null, guild);

        // Act
        var displayInfo = state.GetMuteDisplayInfo(mute);

        // Assert
        Assert.NotNull(displayInfo);
        Assert.NotEmpty(displayInfo.Username);
        Assert.NotEmpty(displayInfo.Reason);
        Assert.NotEmpty(displayInfo.Duration);
        Assert.Contains("Test reason", displayInfo.Reason);
    }

    [Fact]
    public void MuteListPaginatorState_WithEmptyList_HasMinimumOnePage()
    {
        // Arrange
        var guild = CreateTestGuild();

        // Act
        var state = new MuteListPaginatorState(Array.Empty<Mute>(), null, guild);

        // Assert
        Assert.Equal(0, state.TotalMutes);
        Assert.Equal(1, state.TotalPages); // Minimum 1 page even if empty
    }

    #endregion

    #region UserHistoryPaginatorState Tests

    [Fact]
    public void UserHistoryPaginatorState_CalculatesTotalPages_Correctly()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(25); // 25 reprimands
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();

        // Act
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Assert
        Assert.Equal(25, state.TotalReprimands);
        Assert.Equal(5, state.TotalPages); // Collapsed (5 per page): Math.Ceiling(25/5) = 5
        Assert.False(state.IsChronologicalExpanded); // Should default to collapsed
    }

    [Fact]
    public void UserHistoryPaginatorState_DefaultsToCollapsed()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(10);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();

        // Act
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Assert
        Assert.False(state.IsChronologicalExpanded); // Should start collapsed (5 items per page)
        Assert.False(state.IsGroupedExpanded); // Should start collapsed (truncated reasons)
        Assert.False(state.IsGrouped); // Should start in chronological mode
    }

    [Fact]
    public void UserHistoryPaginatorState_ToggleExpanded_ChangesPageCount()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(25); // 25 reprimands
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Act - Toggle from collapsed (5/page) to expanded (10/page)
        state.ToggleExpanded();

        // Assert
        Assert.True(state.IsChronologicalExpanded); // Now expanded
        Assert.Equal(3, state.TotalPages); // Math.Ceiling(25/10) = 3 pages
        Assert.True(state.PageCountChanged); // Flag should be set

        // Act - Toggle back to collapsed
        state.ToggleExpanded();

        // Assert
        Assert.False(state.IsChronologicalExpanded); // Back to collapsed
        Assert.Equal(5, state.TotalPages); // Math.Ceiling(25/5) = 5 pages
        Assert.True(state.PageCountChanged);
    }

    [Fact]
    public void UserHistoryPaginatorState_ToggleGrouped_ChangesPageCountToOne()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(10);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);
        var initialPageCount = state.TotalPages; // Should be 2 (10 items / 5 per page)

        // Act - Toggle to grouped mode
        state.ToggleGrouped();

        // Assert - Grouped mode shows all reprimands on one page
        Assert.True(state.IsGrouped);
        Assert.Equal(1, state.TotalPages); // All reprimands on one page
        Assert.True(state.PageCountChanged); // Flag should be set

        // Act - Toggle back to chronological
        state.ToggleGrouped();

        // Assert - Back to paginated view
        Assert.False(state.IsGrouped);
        Assert.Equal(initialPageCount, state.TotalPages); // Back to 2 pages
        Assert.True(state.PageCountChanged);
    }

    [Fact]
    public void UserHistoryPaginatorState_GetReprimandsForPage_ReturnsCorrectReprimands()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(25);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Act - Collapsed mode (5 per page, default)
        var page0Collapsed = state.GetReprimandsForPage(0).ToList();
        var page1Collapsed = state.GetReprimandsForPage(1).ToList();
        var page4Collapsed = state.GetReprimandsForPage(4).ToList();

        // Assert - Collapsed: 5 pages with 5 items each
        Assert.Equal(5, page0Collapsed.Count);
        Assert.Equal(5, page1Collapsed.Count);
        Assert.Equal(5, page4Collapsed.Count); // Last page

        // Act - Toggle to expanded (10 per page)
        state.ToggleExpanded();
        var page0Expanded = state.GetReprimandsForPage(0).ToList();
        var page2Expanded = state.GetReprimandsForPage(2).ToList();

        // Assert - Expanded: 3 pages, last has 5 items
        Assert.Equal(10, page0Expanded.Count);
        Assert.Equal(5, page2Expanded.Count); // Last page has only 5 reprimands
    }

    [Fact]
    public void UserHistoryPaginatorState_UpdateFilters_RecalculatesProperly()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(10);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Act
        state.UpdateFilters(null, LogReprimandType.Warning);

        // Assert
        Assert.Equal(LogReprimandType.Warning, state.TypeFilter);
        // Total should remain the same since we're not actually filtering in test data
        Assert.True(state.TotalReprimands >= 0);
    }

    [Fact]
    public void UserHistoryPaginatorState_UpdateData_RefreshesReprimands()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var initialReprimands = CreateTestReprimands(10);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, initialReprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Act
        var newReprimands = CreateTestReprimands(5);
        state.UpdateData(newReprimands);

        // Assert
        Assert.Equal(5, state.AllReprimands.Count);
    }

    [Fact]
    public void UserHistoryPaginatorState_GetReprimandDisplayInfo_FormatsCorrectly()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimand = new Warning(1, TimeSpan.FromDays(30), new ReprimandDetails(
            CreateMockUser(123), CreateMockGuildUser(456), "Test warning"));
        reprimand.Id = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity,
            new[] { reprimand }, null, LogReprimandType.All, guild, user, imageBytes);

        // Act
        var displayInfo = state.GetReprimandDisplayInfo(reprimand);

        // Assert
        Assert.NotNull(displayInfo);
        Assert.Equal("Warning", displayInfo.Type);
        Assert.Equal("12345678", displayInfo.ShortId); // First segment of GUID
        Assert.NotEmpty(displayInfo.DateShort); // Discord timestamp format
        Assert.NotEmpty(displayInfo.TimeShort); // Discord timestamp format
        Assert.NotEmpty(displayInfo.RelativeTime); // Discord timestamp format
        Assert.Contains("Test warning", displayInfo.Reason);
        // Moderator should be either a mention (<@id>) or "System" if no moderator
        Assert.NotEmpty(displayInfo.Moderator);
    }

    [Fact]
    public void UserHistoryPaginatorState_ImageBytes_StoredCorrectly()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(5);
        var guild = CreateTestGuild();
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Assert
        Assert.Equal(imageBytes, state.HistoryImageBytes);
        Assert.Equal(5, state.HistoryImageBytes.Length);
    }

    [Fact]
    public void UserHistoryPaginatorState_RequestedBy_StoredCorrectly()
    {
        // Arrange
        var user = CreateMockUser(123);
        var requestedBy = CreateMockUser(999);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(5);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();

        // Act
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, requestedBy, imageBytes);

        // Assert
        Assert.Equal(requestedBy, state.RequestedBy);
        Assert.Equal(999UL, state.RequestedBy.Id);
    }

    [Fact]
    public void UserHistoryPaginatorState_IndependentExpandStates()
    {
        // Arrange
        var user = CreateMockUser(123);
        var userEntity = CreateTestUserEntity(123);
        var reprimands = CreateTestReprimands(25);
        var guild = CreateTestGuild();
        var imageBytes = Array.Empty<byte>();
        var state = new UserHistoryPaginatorState(user, userEntity, reprimands,
            null, LogReprimandType.All, guild, user, imageBytes);

        // Assert - Both start collapsed
        Assert.False(state.IsChronologicalExpanded);
        Assert.False(state.IsGroupedExpanded);
        Assert.Equal(5, state.TotalPages); // 5 items per page when collapsed

        // Act - Expand chronological only
        state.ToggleExpanded();

        // Assert - Chronological expanded, grouped still collapsed
        Assert.True(state.IsChronologicalExpanded);
        Assert.False(state.IsGroupedExpanded);
        Assert.Equal(3, state.TotalPages); // Page count changed to 10 per page

        // Act - Toggle grouped independently
        state.IsGroupedExpanded = !state.IsGroupedExpanded;

        // Assert - Both now expanded
        Assert.True(state.IsChronologicalExpanded);
        Assert.True(state.IsGroupedExpanded);

        // Act - Collapse chronological back
        state.ToggleExpanded();

        // Assert - Chronological collapsed, grouped still expanded
        Assert.False(state.IsChronologicalExpanded);
        Assert.True(state.IsGroupedExpanded);
        Assert.Equal(5, state.TotalPages); // Page count changed back
    }

    #endregion

    #region Helper Methods

    private static List<Mute> CreateTestMutes(int count)
    {
        var mutes = new List<Mute>();
        for (int i = 0; i < count; i++)
        {
            var details = new ReprimandDetails(
                CreateMockUser((ulong)(1000 + i)),
                CreateMockGuildUser((ulong)(2000 + i)),
                $"Test mute reason {i}");

            var mute = new Mute(TimeSpan.FromHours(i + 1), details)
            {
                Id = Guid.NewGuid()
            };

            mutes.Add(mute);
        }
        return mutes;
    }

    private static List<Reprimand> CreateTestReprimands(int count)
    {
        var reprimands = new List<Reprimand>();
        for (int i = 0; i < count; i++)
        {
            var details = new ReprimandDetails(
                CreateMockUser((ulong)(1000 + i)),
                CreateMockGuildUser((ulong)(2000 + i)),
                $"Test reprimand reason {i}");

            Reprimand reprimand = (i % 3) switch
            {
                0 => new Warning((uint)(i + 1), TimeSpan.FromDays(30), details),
                1 => new Mute(TimeSpan.FromHours(i + 1), details),
                _ => new Note(details)
            };

            reprimand.Id = Guid.NewGuid();
            reprimands.Add(reprimand);
        }
        return reprimands;
    }

    private static GuildEntity CreateTestGuild()
    {
        return new GuildEntity(123456789)
        {
            ModerationCategories = new List<ModerationCategory>()
        };
    }

    private static GuildUserEntity CreateTestUserEntity(ulong userId)
    {
        return new GuildUserEntity(userId, 123456789);
    }

    private static IUser CreateMockUser(ulong id)
    {
        var mock = new Mock<IUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Username).Returns($"User{id}");
        mock.Setup(u => u.Mention).Returns($"<@{id}>");
        mock.Setup(u => u.CreatedAt).Returns(DateTimeOffset.UtcNow.AddDays(-30));
        mock.Setup(u => u.GetDisplayAvatarUrl(It.IsAny<ImageFormat>(), It.IsAny<ushort>()))
            .Returns($"https://cdn.discordapp.com/avatars/{id}/avatar.png");
        mock.Setup(u => u.GetDefaultAvatarUrl()).Returns("https://cdn.discordapp.com/embed/avatars/0.png");
        return mock.Object;
    }

    private static IGuildUser CreateMockGuildUser(ulong id)
    {
        var guildMock = new Mock<IGuild>();
        guildMock.Setup(g => g.Id).Returns(123456789);
        
        var mock = new Mock<IGuildUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Username).Returns($"GuildUser{id}");
        mock.Setup(u => u.Guild).Returns(guildMock.Object);
        return mock.Object;
    }

    #endregion
}

