using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Services.Core.Messages;
using HuTao.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace HuTao.Tests.Services.Logging;

/// <summary>
///     A reaction is saved only when the server logs that kind of reaction, because nothing else reads the saved copy.
/// </summary>
public class ReactionLogRetentionTests
{
    private const ulong GuildId = 800;
    private const ulong ChannelId = 801;
    private const ulong AuthorId = 802;
    private const ulong ReactorId = 803;
    private const ulong MessageId = 804;

    private static HuTaoContext NewContext()
        => new(new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static LoggingService NewService(HuTaoContext db)
        => new(new DiscordSocketClient(), new System.Net.Http.HttpClient(), db, new MemoryCache(new MemoryCacheOptions()));

    private static async Task SeedGuildAsync(HuTaoContext db, params LogType[] logged)
    {
        var guild = new GuildEntity(GuildId);
        if (logged.Length > 0)
        {
            guild.LoggingRules = new LoggingRules
            {
                LoggingChannels = logged.Select(t => new EnumChannel<LogType>(t, Mock.Of<IChannel>(c => c.Id == 9))).ToList()
            };
        }

        db.Guilds.Add(guild);
        await db.SaveChangesAsync();
    }

    private static IGuildUser NewMember(IGuild guild, ulong id)
    {
        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.IsBot).Returns(false);
        user.SetupGet(u => u.Guild).Returns(guild);
        user.SetupGet(u => u.GuildId).Returns(GuildId);
        return user.Object;
    }

    // Discord.Net builds reactions from gateway events only, so the test calls its internal constructor and fills each
    // parameter by name. Parameters it does not name get their default.
    private static SocketReaction NewReaction(ISocketMessageChannel channel, IUser reactor, IEmote emote)
    {
        var constructor = typeof(SocketReaction)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var arguments = constructor.GetParameters().Select(p => p.Name switch
        {
            "channel"   => channel,
            "messageId" => MessageId,
            "userId"    => reactor.Id,
            "user"      => new Optional<IUser>(reactor),
            "emoji"     => emote,
            _           => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null
        }).ToArray();

        return (SocketReaction) constructor.Invoke(arguments);
    }

    private static (Cacheable<IUserMessage, ulong> Message, Cacheable<IMessageChannel, ulong> Channel, SocketReaction
        Reaction) NewReactionEvent()
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(GuildId);

        var channel = new Mock<ISocketMessageChannel>();
        channel.SetupGet(c => c.Id).Returns(ChannelId);
        channel.As<IGuildChannel>().SetupGet(c => c.Guild).Returns(guild.Object);
        channel.As<IGuildChannel>().SetupGet(c => c.GuildId).Returns(GuildId);
        channel.As<INestedChannel>().SetupGet(c => c.Guild).Returns(guild.Object);
        channel.As<INestedChannel>().SetupGet(c => c.Id).Returns(ChannelId);

        var message = new Mock<IUserMessage>();
        message.SetupGet(m => m.Id).Returns(MessageId);
        message.SetupGet(m => m.Author).Returns(NewMember(guild.Object, AuthorId));
        message.SetupGet(m => m.Channel).Returns(channel.Object);
        message.SetupGet(m => m.Reactions).Returns(new Dictionary<IEmote, ReactionMetadata>());

        var reaction = NewReaction(channel.Object, NewMember(guild.Object, ReactorId), new Emoji("👍"));

        return (Cached<IUserMessage>(message.Object, MessageId), Cached<IMessageChannel>(channel.Object, ChannelId), reaction);
    }

    // Discord.Net 3.18 keeps the constructor of a cached entity internal.
    private static Cacheable<T, ulong> Cached<T>(T value, ulong id) where T : class, IEntity<ulong>
        => (Cacheable<T, ulong>) Activator.CreateInstance(typeof(Cacheable<T, ulong>),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            [value, id, true, (Func<Task<T>>) (() => Task.FromResult(value))], null)!;

    [Fact]
    public async Task ReactionAdded_InServerThatDoesNotLogReactions_SavesNothing()
    {
        await using var db = NewContext();
        await SeedGuildAsync(db, LogType.MessageDeleted);
        var (message, channel, reaction) = NewReactionEvent();

        await NewService(db).LogAsync(new ReactionAddedNotification(message, channel, reaction), default);

        Assert.Empty(db.Set<ReactionLog>());
    }

    [Fact]
    public async Task ReactionRemoved_InServerThatDoesNotLogReactions_SavesNothing()
    {
        await using var db = NewContext();
        await SeedGuildAsync(db, LogType.MessageDeleted);
        var (message, channel, reaction) = NewReactionEvent();

        await NewService(db).LogAsync(new ReactionRemovedNotification(message, channel, reaction), default);

        Assert.Empty(db.Set<ReactionDeleteLog>());
    }

    [Fact]
    public async Task ReactionAdded_InServerThatLogsAddedReactions_IsSaved()
    {
        await using var db = NewContext();
        await SeedGuildAsync(db, LogType.ReactionAdded);
        var (message, channel, reaction) = NewReactionEvent();

        await NewService(db).LogAsync(new ReactionAddedNotification(message, channel, reaction), default);

        Assert.Equal(ReactorId, (await db.Set<ReactionLog>().SingleAsync()).UserId);
    }

    [Fact]
    public async Task ReactionRemoved_InServerThatLogsRemovedReactions_IsSaved()
    {
        await using var db = NewContext();
        await SeedGuildAsync(db, LogType.ReactionRemoved);
        var (message, channel, reaction) = NewReactionEvent();

        await NewService(db).LogAsync(new ReactionRemovedNotification(message, channel, reaction), default);

        Assert.Equal(ReactorId, (await db.Set<ReactionDeleteLog>().SingleAsync()).UserId);
    }
}
