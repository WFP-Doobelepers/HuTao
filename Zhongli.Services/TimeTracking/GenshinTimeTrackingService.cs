using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Cronos;
using Discord;
using Discord.WebSocket;
using Hangfire;
using Humanizer;
using Humanizer.Localisation;
using Mapster;
using Zhongli.Data.Models.TimeTracking;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.TimeTracking;

public class GenshinTimeTrackingService
{
    public enum ServerRegion
    {
        America,
        Europe,
        Asia,
        SAR
    }

    private readonly DiscordSocketClient _client;

    public GenshinTimeTrackingService(DiscordSocketClient client) { _client = client; }

    private static Dictionary<ServerRegion, (string Name, int Offset)> ServerOffsets { get; } = new()
    {
        [ServerRegion.America] = ("NA", -5),
        [ServerRegion.Europe]  = ("EU", 1),
        [ServerRegion.Asia]    = ("ASIA", 8),
        [ServerRegion.SAR]     = ("SAR", 8)
    };

    [AutomaticRetry(Attempts = 0)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task UpdateChannelAsync(ulong guildId, ulong channelId, ServerRegion region)
    {
        var guild = await GetGuildAsync(guildId);
        if (guild is null) return;

        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null) return;

        await TrackRegionAsync(channel, region);
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task UpdateMessageAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        var guild = await GetGuildAsync(guildId);
        if (guild is null) return;

        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null) return;

        if (await channel.GetMessageAsync(messageId) is not IUserMessage message)
            return;

        var embed = new EmbedBuilder()
            .WithTitle("Server Status")
            .WithGuildAsAuthor(guild, AuthorOptions.UseFooter)
            .WithCurrentTimestamp();

        AddRegion(embed, ServerRegion.America, "md");
        AddRegion(embed, ServerRegion.Europe, string.Empty);
        AddRegion(embed, ServerRegion.Asia, "cs");
        AddRegion(embed, ServerRegion.SAR, "fix");

        await message.ModifyAsync(m =>
        {
            m.Content = string.Empty;
            m.Embed   = embed.Build();
        });
    }

    public void TrackGenshinTime(GenshinTimeTrackingRules rules)
    {
        var serverStatus = rules.ServerStatus?.Adapt<MessageTimeTracking>();
        if (serverStatus is not null)
        {
            var id = serverStatus.Id.ToString();
            RecurringJob.AddOrUpdate(id, ()
                    => UpdateMessageAsync(
                        serverStatus.GuildId,
                        serverStatus.ChannelId,
                        serverStatus.MessageId),
                Cron.Minutely);

            RecurringJob.Trigger(id);
        }

        AddJob(rules.AmericaChannel, ServerRegion.America);
        AddJob(rules.EuropeChannel, ServerRegion.America);
        AddJob(rules.AsiaChannel, ServerRegion.America);
        AddJob(rules.SARChannel, ServerRegion.America);
    }

    private static DateTimeOffset GetDailyReset(int offset)
    {
        var baseUtcOffset = TimeSpan.FromHours(offset);
        var timezone = TimeZoneInfo.CreateCustomTimeZone(offset.ToString(), baseUtcOffset, null, null);
        var expression = CronExpression.Parse(Cron.Daily(4));
        var next = expression.GetNextOccurrence(DateTime.UtcNow, timezone);

        return next!.Value;
    }

    private static DateTimeOffset GetTime(int offset) => DateTimeOffset.UtcNow
        .ToOffset(TimeSpan.FromHours(offset));

    private static DateTimeOffset GetWeeklyReset(int offset)
    {
        var baseUtcOffset = TimeSpan.FromHours(offset);
        var timezone = TimeZoneInfo.CreateCustomTimeZone(offset.ToString(), baseUtcOffset, null, null);
        var expression = CronExpression.Parse(Cron.Weekly(DayOfWeek.Monday, 4));
        var next = expression.GetNextOccurrence(DateTime.UtcNow, timezone);

        return next!.Value;
    }

    private static Task TrackRegionAsync(IGuildChannel channel, ServerRegion region)
    {
        var (name, offset) = ServerOffsets[region];
        return channel.ModifyAsync(c => c.Name = $"{name}: {GetTime(offset)}");
    }

    private async Task<IGuild?> GetGuildAsync(ulong guildId) =>
        _client.GetGuild(guildId) as IGuild
        ?? await _client.Rest.GetGuildAsync(guildId);

    private void AddJob(ChannelTimeTracking? tracking, ServerRegion region)
    {
        if (tracking is null) return;

        var id = tracking.Id.ToString();
        RecurringJob.AddOrUpdate(id, ()
                => UpdateChannelAsync(
                    tracking.GuildId,
                    tracking.ChannelId,
                    region),
            "*/5 * * * *");

        RecurringJob.Trigger(id);
    }

    private static void AddRegion(EmbedBuilder builder, ServerRegion region, string language)
    {
        var (name, offset) = ServerOffsets[region];
        var time = GetTime(offset);
        builder
            .AddField($"{region.Humanize()} Time", Format.Bold(Format.Code($"# {name} {time}", language)))
            .AddField("Daily",
                $"Resets in {Format.Bold(GetDailyReset(offset).TimeLeft().Humanize(4, minUnit: TimeUnit.Minute))}",
                true)
            .AddField("Weekly",
                $"Resets in {Format.Bold(GetWeeklyReset(offset).TimeLeft().Humanize(4, minUnit: TimeUnit.Minute))}",
                true);
    }
}