using System;
using System.Threading.Tasks;
using Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;
using HuTao.Tests.Testing;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

// A button or form carries the ID of a mute, a timeout or a trigger, and a member of another server can send one with
// any ID. The panels look the ID up only among this server's records.
public class GuildRecordsTests
{
    private const ulong OtherGuildId = 990;

    [Fact]
    public async Task AReprimandOfThisServer_IsFound()
    {
        var fixture = new AuthorizationFixture();
        var mute = new Mute(TimeSpan.FromHours(1), new ReprimandDetails(fixture.User(77), fixture.User(300), "spam"));
        fixture.AddReprimand(mute);

        Assert.Same(mute, await fixture.Db.ReprimandAsync<Mute>(fixture.Guild.Id, mute.Id));
    }

    [Fact]
    public async Task AReprimandOfAnotherServer_IsNotFound()
    {
        var fixture = new AuthorizationFixture();
        var mute = new Mute(TimeSpan.FromHours(1), new ReprimandDetails(fixture.User(77), OtherModerator(), "spam"));
        fixture.AddReprimand(mute);

        Assert.Null(await fixture.Db.ReprimandAsync<Mute>(fixture.Guild.Id, mute.Id));
    }

    [Fact]
    public void ATriggerOfThisServer_IsFound_AndAnotherIsNot()
    {
        var fixture = new AuthorizationFixture();
        var trigger = new ReplyConfiguration(null, Mock.Of<IAutoConfigurationOptions>()) { Id = Guid.NewGuid() };
        fixture.Guild.ModerationRules = new ModerationRules();
        fixture.Guild.ModerationRules.Triggers.Add(trigger);

        Assert.Same(trigger, fixture.Guild.Trigger<AutoConfiguration>(trigger.Id));
        Assert.Null(fixture.Guild.Trigger<AutoConfiguration>(Guid.NewGuid()));
    }

    private static IGuildUser OtherModerator()
    {
        var guild = Mock.Of<IGuild>(g => g.Id == OtherGuildId);
        return Mock.Of<IGuildUser>(u => u.Id == 301 && u.Guild == guild && u.GuildId == OtherGuildId);
    }
}
