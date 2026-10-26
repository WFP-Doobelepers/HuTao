using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit;

namespace HuTao.Tests.Services.Logging;

// The set-based delete of the reaction and deletion logs, on a real PostgreSQL where the foreign keys fire. Opt-in like
// PostgresTrackRaceTests: every test no-ops unless HUTAO_PG_RACE=1 and HUTAO_PG_RACE_CS names the scratch database
// hutao_race_check on localhost. The rows are dated in the year 2000 and the cutoff is in 2001, so no other row
// in the scratch database is old enough to go.
public class PostgresLogRetentionTests
{
    private const string ScratchDatabase = "hutao_race_check";

    private static readonly DateTimeOffset Cutoff = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Old = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("HUTAO_PG_RACE") == "1"
            ? Environment.GetEnvironmentVariable("HUTAO_PG_RACE_CS")
            : null;

    private static HuTaoContext NewDb(string cs) => new(new DbContextOptionsBuilder<HuTaoContext>().UseNpgsql(cs).Options);

    private static T NewLog<T>(ulong guildId, ulong userId, DateTimeOffset logDate, ulong messageId, ActionDetails? deletedBy)
        where T : DeleteLog, IMessageEntity
    {
        var log = (T) Activator.CreateInstance(typeof(T), nonPublic: true)!;
        (log.GuildId, log.ChannelId, log.MessageId, log.UserId) = (guildId, 1, messageId, userId);
        log.LogDate = logDate;
        log.Action  = deletedBy?.ToModerationAction();
        return log;
    }

    // A bulk deletion whose messages are dated across the cutoff, one of them listed by a filter reprimand, and a batch
    // of one row, so that a bulk deletion is picked alone while its messages still point at it.
    [Fact]
    public async Task OldLogs_AreDeletedWithTheirMessagesAndActions_AndAReprimandKeepsTheOneItLists()
    {
        if (ConnectionString is not { } cs) return;
        var builder = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal(ScratchDatabase, builder.Database);
        Assert.Contains(builder.Host, new[] { "localhost", "127.0.0.1", "::1" });

        var guildId = (ulong) Random.Shared.NextInt64(1L << 40, 1L << 60);
        var (userId, moderatorId) = (guildId + 1, guildId + 2);
        var moderator = new ActionDetails(moderatorId, guildId, "fixture");
        var emoji = new EmojiEntity(new Emoji($"fixture-{guildId}"));

        try
        {
            await using (var seed = NewDb(cs))
            {
                seed.Guilds.Add(new GuildEntity(guildId));
                seed.Users.AddRange(new GuildUserEntity(userId, guildId), new GuildUserEntity(moderatorId, guildId));

                var reaction = (ReactionLog) Activator.CreateInstance(typeof(ReactionLog), nonPublic: true)!;
                (reaction.GuildId, reaction.ChannelId, reaction.MessageId, reaction.UserId) = (guildId, 1, 1, userId);
                (reaction.LogDate, reaction.Emote) = (Old, emoji);
                var kept = (ReactionLog) Activator.CreateInstance(typeof(ReactionLog), nonPublic: true)!;
                (kept.GuildId, kept.ChannelId, kept.MessageId, kept.UserId) = (guildId, 1, 2, userId);
                (kept.LogDate, kept.Emote) = (DateTimeOffset.UtcNow, emoji);
                seed.AddRange(reaction, kept);

                var removedReaction = (ReactionDeleteLog) Activator.CreateInstance(typeof(ReactionDeleteLog), nonPublic: true)!;
                (removedReaction.GuildId, removedReaction.ChannelId, removedReaction.MessageId, removedReaction.UserId)
                    = (guildId, 1, 3, userId);
                (removedReaction.LogDate, removedReaction.Emote, removedReaction.Action)
                    = (Old, emoji, moderator.ToModerationAction());
                seed.Add(removedReaction);

                seed.Add(NewLog<MessageDeleteLog>(guildId, userId, Old, 4, moderator));

                var listed = NewLog<MessageDeleteLog>(guildId, userId, Old.AddSeconds(2), 7, moderator);
                var bulk = (MessagesDeleteLog) Activator.CreateInstance(typeof(MessagesDeleteLog), nonPublic: true)!;
                (bulk.GuildId, bulk.ChannelId, bulk.LogDate, bulk.Action) = (guildId, 1, Old, moderator.ToModerationAction());
                bulk.Messages =
                [
                    NewLog<MessageDeleteLog>(guildId, userId, Old.AddSeconds(1), 5, moderator),
                    NewLog<MessageDeleteLog>(guildId, userId, Cutoff.AddDays(1), 6, moderator),
                    listed
                ];
                seed.Add(bulk);

                var member = Mock.Of<IGuildUser>(u => u.Id == userId);
                var mod = Mock.Of<IGuildUser>(u => u.Id == moderatorId && u.Guild == Mock.Of<IGuild>(g => g.Id == guildId));
                seed.Add(new Filtered(new List<MessageDeleteLog> { listed }, null, new ReprimandDetails(member, mod, "fixture")));

                await seed.SaveChangesAsync();
            }

            await using var db = NewDb(cs);
            var deleted = await new MessageLogCleanupService(db).DeleteLogsOlderThanAsync(Cutoff, batchSize: 1);

            await using var check = NewDb(cs);
            Assert.Equal(6, deleted);
            Assert.Equal(2UL, (await check.Set<ReactionLog>().SingleAsync(l => l.GuildId == guildId)).MessageId);
            Assert.Empty(await check.Set<ReactionDeleteLog>().Where(l => l.GuildId == guildId).ToListAsync());
            Assert.Empty(await check.Set<MessagesDeleteLog>().Where(l => l.GuildId == guildId).ToListAsync());

            var left = await check.Set<MessageDeleteLog>().SingleAsync(l => l.GuildId == guildId);
            Assert.Equal(7UL, left.MessageId);
            Assert.Null(check.Entry(left).Property<Guid?>("MessagesDeleteLogId").CurrentValue);
            Assert.Single(await check.Set<Filtered>().Where(f => f.GuildId == guildId).ToListAsync());

            // What is left is the action of the reprimand and the action of the message it lists.
            Assert.Equal(2, await check.Set<ModerationAction>().CountAsync(a => a.GuildId == guildId));
        }
        finally
        {
            await using var db = NewDb(cs);
            var wipe = $"""
                update "DeleteLog" set "MessagesDeleteLogId" = null, "FilteredId" = null where "GuildId" = {guildId};
                delete from "DeleteLog" where "GuildId" = {guildId};
                delete from "Reprimand" where "GuildId" = {guildId};
                delete from "ModerationAction" where "GuildId" = {guildId};
                delete from "ReactionLog" where "GuildId" = {guildId};
                delete from "ReactionEntity" where "Name" = 'fixture-{guildId}';
                delete from "Users" where "GuildId" = {guildId};
                delete from "Guilds" where "Id" = {guildId};
                """;
            await db.Database.ExecuteSqlRawAsync(wipe);
        }
    }
}
