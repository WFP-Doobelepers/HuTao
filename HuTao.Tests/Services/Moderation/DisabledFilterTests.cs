using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Moderation;

// A moderator who switches a message filter off with `auto toggle` expects it to stop acting.
public class DisabledFilterTests
{
    private const ulong GuildId = 1;
    private const ulong ChannelId = 10;
    private const ulong MemberId = 40;

    private static readonly string Cooldown = $"IsUserCooldown.{MemberId}";

    [Fact]
    public async Task ADisabledFilter_DoesNotActOnAMatchingMessage()
    {
        var (behavior, cache, message) = Harness(active: false);

        await behavior.ProcessMessage(message);

        Assert.False(cache.TryGetValue(Cooldown, out _));
    }

    // The same message reaches the reprimand while the filter is on: the filter starts the member's cooldown. The
    // harness has no bot member, so the reprimand stops after that.
    [Fact]
    public async Task AnActiveFilter_ActsOnTheSameMessage()
    {
        var (behavior, cache, message) = Harness(active: true);

        await Record.ExceptionAsync(() => behavior.ProcessMessage(message));

        Assert.True(cache.TryGetValue(Cooldown, out _));
    }

    // The behavior reads messages only from a SocketTextChannel, which Discord.Net builds with internal constructors.
    private static (AutoModerationBehavior Behavior, IMemoryCache Cache, IUserMessage Message) Harness(bool active)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var client = new DiscordSocketClient();
        var socketGuild = (SocketGuild) Activator.CreateInstance(
            typeof(SocketGuild), flags, null, [client, GuildId], null)!;
        var channel = (SocketTextChannel) Activator.CreateInstance(
            typeof(SocketTextChannel), flags, null, [client, ChannelId, socketGuild], null)!;

        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(GuildId);
        var member = new Mock<IGuildUser>();
        member.SetupGet(u => u.Id).Returns(MemberId);
        member.SetupGet(u => u.Guild).Returns(guild.Object);
        member.SetupGet(u => u.RoleIds).Returns([]);
        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(100);
        message.SetupGet(m => m.Author).Returns(member.Object);
        message.SetupGet(m => m.Channel).Returns(channel);
        message.SetupGet(m => m.Content).Returns("hello");
        message.SetupGet(m => m.Timestamp).Returns(DateTimeOffset.Now);

        var filter = new MessageConfiguration(null, new Options()) { IsActive = active };
        var rules = new ModerationRules();
        rules.Triggers.Add(filter);

        var db = new HuTaoContext(new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseLazyLoadingProxies()
            .Options);
        db.Add(new GuildEntity(GuildId) { ModerationRules = rules });
        db.SaveChanges();

        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new AutoModerationBehavior(client, db, cache, null!), cache, message.Object);
    }

    // One message is enough, and a minute of cooldown marks the moment the filter acts.
    private sealed class Options : IAutoConfigurationOptions
    {
        public ModerationCategory? Category { get; set; }

        public TriggerMode Mode { get; set; } = TriggerMode.Retroactive;

        public uint Amount { get; set; } = 1;

        public bool DeleteMessages { get; set; }

        public bool GlobalFilter { get; set; }

        public bool MentionCountDuplicates { get; set; }

        public bool MentionCountInvalid { get; set; }

        public bool MentionCountRoleMembers { get; set; }

        public bool NewLineBlankOnly { get; set; }

        public double DuplicatePercentage { get; set; }

        public DuplicateConfiguration.DuplicateType DuplicateType { get; set; }

        public int DuplicateTolerance { get; set; }

        public int MinimumLength { get; set; }

        public string? Reason { get; set; }

        public TimeSpan TimePeriod { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan? Cooldown { get; set; } = TimeSpan.FromMinutes(1);
    }
}
