using System;
using Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Utilities;

public class ReprimandBadgeHelperTests
{
    [Fact]
    public void TypeBadges_AreDiscordRenderable_DoNotUseMarkdownImages()
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(923991820868911184ul);

        var moderator = new Mock<IGuildUser>();
        moderator.SetupGet(m => m.Id).Returns(111ul);
        moderator.SetupGet(m => m.Guild).Returns(guild.Object);

        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(222ul);

        var details = new ReprimandDetails(user.Object, moderator.Object, "Spamming in #general");
        var reprimand = new Warning(1, TimeSpan.FromDays(7), details);

        var typeBadges = ReprimandBadgeHelper.GetTypeBadges(reprimand);
        var statusBadge = ReprimandBadgeHelper.GetStatusBadge(reprimand.Status);

        Assert.DoesNotContain("![", typeBadges, StringComparison.Ordinal);
        Assert.DoesNotContain("http", typeBadges, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(typeBadges));

        Assert.DoesNotContain("![", statusBadge, StringComparison.Ordinal);
        Assert.DoesNotContain("http", statusBadge, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(statusBadge));
    }

    [Fact]
    public void TypeAndStatusColors_RemainHexStrings()
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(923991820868911184ul);

        var moderator = new Mock<IGuildUser>();
        moderator.SetupGet(m => m.Id).Returns(111ul);
        moderator.SetupGet(m => m.Guild).Returns(guild.Object);

        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(222ul);

        var details = new ReprimandDetails(user.Object, moderator.Object, "Spamming in #general");
        var reprimand = new Warning(1, TimeSpan.FromDays(7), details);

        var typeColor = ReprimandBadgeHelper.GetTypeColor(reprimand);
        var statusColor = ReprimandBadgeHelper.GetStatusColor(reprimand.Status);

        Assert.Matches("^[0-9A-Fa-f]{6}$", typeColor);
        Assert.Matches("^[0-9A-Fa-f]{6}$", statusColor);
    }
}

