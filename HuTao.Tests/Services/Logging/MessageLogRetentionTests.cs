using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using Attachment = HuTao.Data.Models.Discord.Message.Attachment;

namespace HuTao.Tests.Services.Logging;

/// <summary>
///     The logs of deleted messages and of reactions go after 30 days.
/// </summary>
public class MessageLogRetentionTests
{
    private const ulong GuildId = 700;
    private const ulong ChannelId = 701;
    private const ulong UserId = 702;

    private static HuTaoContext NewContext()
        => new(new DbContextOptionsBuilder<HuTaoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

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

    private static T NewLog<T>(DateTimeOffset logDate, ulong messageId, ActionDetails? deletedBy = null)
        where T : class
    {
        // The reaction logs take a gateway reaction in their public constructors, so the test fills the fields itself.
        var log = (T) Activator.CreateInstance(typeof(T), nonPublic: true)!;
        switch (log)
        {
            case ReactionLog reaction:
                (reaction.GuildId, reaction.ChannelId, reaction.MessageId, reaction.UserId) = (GuildId, ChannelId, messageId, UserId);
                reaction.LogDate = logDate;
                reaction.Emote   = new EmojiEntity(new Emoji("👍"));
                break;
            case ReactionDeleteLog reaction:
                (reaction.GuildId, reaction.ChannelId, reaction.MessageId, reaction.UserId) = (GuildId, ChannelId, messageId, UserId);
                reaction.LogDate = logDate;
                reaction.Emote   = new EmojiEntity(new Emoji("👎"));
                reaction.Action  = deletedBy?.ToModerationAction();
                break;
            case MessageDeleteLog message:
                (message.GuildId, message.ChannelId, message.MessageId, message.UserId) = (GuildId, ChannelId, messageId, UserId);
                message.LogDate = logDate;
                message.Action  = deletedBy?.ToModerationAction();
                break;
        }

        return log;
    }

    // A deletion log and a reaction log hold who wrote or reacted to which message. They go after the same 30 days as
    // the saved messages. A deletion that a filter reprimand lists stays with the reprimand.
    [Fact]
    public async Task Cleanup_DeletesOldReactionAndDeletionLogs_AndKeepsNewOnesAndTheOnesAReprimandLists()
    {
        await using var db = NewContext();
        await SeedGuildAsync(db);

        var old = DateTimeOffset.UtcNow.AddDays(-31);
        var recent = DateTimeOffset.UtcNow.AddDays(-1);
        var moderator = new ActionDetails(UserId + 1, GuildId, "spam");

        db.Add(NewLog<ReactionLog>(old, 1));
        db.Add(NewLog<ReactionLog>(recent, 2));
        db.Add(NewLog<ReactionDeleteLog>(old, 3, moderator));
        db.Add(NewLog<MessageDeleteLog>(old, 4, moderator));
        db.Add(NewLog<MessageDeleteLog>(recent, 5, moderator));

        var bulk = (MessagesDeleteLog) Activator.CreateInstance(typeof(MessagesDeleteLog), nonPublic: true)!;
        (bulk.GuildId, bulk.ChannelId, bulk.LogDate, bulk.Action) = (GuildId, ChannelId, old, moderator.ToModerationAction());
        bulk.Messages = [NewLog<MessageDeleteLog>(old, 6, moderator), NewLog<MessageDeleteLog>(old, 7, moderator)];
        db.Add(bulk);

        var listed = NewLog<MessageDeleteLog>(old, 8, moderator);
        db.Add(listed);
        db.Entry(listed).Property("FilteredId").CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync();
        Assert.Equal(7, await db.Set<ModerationAction>().CountAsync());

        var deleted = await new MessageLogCleanupService(db)
            .DeleteLogsOlderThanAsync(DateTimeOffset.UtcNow - MessageLogCleanupService.Retention);

        Assert.Equal(6, deleted);
        Assert.Equal(2UL, (await db.Set<ReactionLog>().SingleAsync()).MessageId);
        Assert.Equal(new ulong[] { 5, 8 }, (await db.Set<MessageDeleteLog>().ToListAsync()).Select(m => m.MessageId).Order());
        Assert.Empty(db.Set<MessagesDeleteLog>());
        Assert.Empty(db.Set<ReactionDeleteLog>());
        Assert.Equal(2, await db.Set<ModerationAction>().CountAsync());
    }
}
