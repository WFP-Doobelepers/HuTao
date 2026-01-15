using System;
using System.Linq;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

public class DemoReprimandSeederTests
{
    [Fact]
    public void GeneratePlan_RespectsPerUserBounds_AndDateWindow()
    {
        var userIds = new ulong[] { 1, 2, 3, 4, 5 };
        var options = new DemoSeedOptions(
            MinReprimandsPerUser: 2,
            MaxReprimandsPerUser: 4,
            DaysBack: 30,
            Seed: 12345);

        var now = new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero);
        var plan = DemoReprimandSeeder.GeneratePlan(userIds, options, now);

        Assert.NotEmpty(plan);

        var perUser = plan.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.Count());
        foreach (var userId in userIds)
        {
            Assert.True(perUser.TryGetValue(userId, out var count));
            Assert.InRange(count, options.MinReprimandsPerUser, options.MaxReprimandsPerUser);
        }

        var minAllowed = now - TimeSpan.FromDays(options.DaysBack + 1);
        Assert.All(plan, p =>
        {
            Assert.InRange(p.ActionDate, minAllowed, now);
        });

        Assert.True(plan.Select(p => p.Kind).Distinct().Count() >= 2);
    }

    [Fact]
    public void GeneratePlan_ExpiredItemsHaveEndedAtAndLength()
    {
        var userIds = new ulong[] { 10, 11, 12, 13, 14, 15, 16 };
        var options = new DemoSeedOptions(
            MinReprimandsPerUser: 5,
            MaxReprimandsPerUser: 8,
            DaysBack: 120,
            Seed: 923991820);

        var now = new DateTimeOffset(2026, 1, 6, 0, 0, 0, TimeSpan.Zero);
        var plan = DemoReprimandSeeder.GeneratePlan(userIds, options, now);

        var expired = plan.Where(p => p.Status == ReprimandStatus.Expired).ToList();
        if (!expired.Any())
            return;

        Assert.All(expired, p =>
        {
            Assert.NotNull(p.Length);
            Assert.NotNull(p.EndedAt);
            Assert.Equal(p.ActionDate + p.Length.Value, p.EndedAt.Value);
        });
    }
}

