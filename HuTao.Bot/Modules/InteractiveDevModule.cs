using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Moderation;

namespace HuTao.Bot.Modules;

[Group("dev", "Developer-only tools")]
public class InteractiveDevModule(DemoReprimandSeeder seeder) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("seed", "Seed demo moderation history data (dev-only).")]
    [RequireTeamMember]
    public async Task SeedAsync(
        [Summary(description: "How many (non-bot) users to seed")]
        int users = 20,
        [Summary(description: "Min reprimands per user")]
        int minPerUser = 3,
        [Summary(description: "Max reprimands per user")]
        int maxPerUser = 10,
        [Summary(description: "How many days back to distribute reprimands")]
        int daysBack = 120,
        [Summary(description: "Clear existing reprimands for the selected users first")]
        bool clearExisting = false)
    {
        if (Context.Guild is null || Context.User is not IGuildUser moderator)
        {
            await RespondAsync("This command can only be used inside a guild.", ephemeral: true);
            return;
        }

        if (Context.Guild.Id != DemoReprimandSeeder.TestGuildId)
        {
            await RespondAsync($"Demo seeding is only enabled for guild `{DemoReprimandSeeder.TestGuildId}`.", ephemeral: true);
            return;
        }

        users = Math.Clamp(users, 1, 50);
        minPerUser = Math.Clamp(minPerUser, 0, 50);
        maxPerUser = Math.Clamp(maxPerUser, minPerUser, 50);
        daysBack = Math.Clamp(daysBack, 1, 365);

        await DeferAsync(true);

        var candidates = Context.Guild.Users
            .Where(u => !u.IsBot)
            .Select(u => (IGuildUser)u)
            .OrderByDescending(u => u.JoinedAt ?? DateTimeOffset.MinValue)
            .Take(users * 2)
            .ToList();

        if (candidates.Count < users)
        {
            var downloaded = await Context.Guild.GetUsersAsync().FlattenAsync();
            candidates = downloaded
                .Where(u => !u.IsBot)
                .OrderByDescending(u => u.JoinedAt ?? DateTimeOffset.MinValue)
                .Take(users * 2)
                .ToList();
        }

        var selected = candidates
            .OrderBy(_ => Guid.NewGuid())
            .Take(users)
            .ToList();

        var options = new DemoSeedOptions(minPerUser, maxPerUser, daysBack)
        {
            ClearExisting = clearExisting
        };

        var result = await seeder.SeedAsync(Context.Guild, moderator, selected, options);

        var summary = new ContainerBuilder()
            .WithTextDisplay(
                $"# Demo seed complete\n\n" +
                $"- **Guild**: `{Context.Guild.Id}`\n" +
                $"- **Users**: {result.UsersSeeded}\n" +
                $"- **Reprimands**: {result.ReprimandsCreated}\n" +
                $"- **Cleared existing**: {result.ClearedExisting}\n\n" +
                "-# Try `/history` on a few users to validate badges + pagination.");

        await FollowupAsync(
            components: new ComponentBuilderV2().WithContainer(summary).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }
}

