using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Cronos;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Hangfire;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.TimeTracking;
using HuTao.Services.Utilities;

namespace HuTao.Services.TimeTracking;

public class GenshinTimeTrackingService(DiscordSocketClient client, HuTaoContext db)
{
    public enum ServerRegion
    {
        America,
        Europe,
        Asia,
        SAR
    }

    private static readonly RequestOptions RequestOptions = new() { Timeout = 15 };

    private static Dictionary<ServerRegion, (string Name, int Offset)> ServerOffsets { get; } = new()
    {
        [ServerRegion.America] = ("NA", -5),
        [ServerRegion.Europe]  = ("EU", 1),
        [ServerRegion.Asia]    = ("ASIA", 8),
        [ServerRegion.SAR]     = ("SAR", 8)
    };

    public async Task TrackGenshinTime(GuildEntity guild)
    {
        var rules = guild.GenshinRules;
        if (rules is null) return;

        if (rules.ServerStatus is not null)
        {
            var id = rules.ServerStatus.Id.ToString();
            var message = await TryGetMessageAsync(
                rules.ServerStatus.GuildId,
                rules.ServerStatus.ChannelId,
                rules.ServerStatus.MessageId);

            if (message is not null)
            {
                RecurringJob.AddOrUpdate(id, ()
                        => UpdateMessageAsync(
                            rules.ServerStatus.GuildId,
                            rules.ServerStatus.ChannelId,
                            rules.ServerStatus.MessageId),
                    Cron.Minutely);
            }
        }

        AddJob(rules.AmericaChannel, ServerRegion.America);
        AddJob(rules.EuropeChannel, ServerRegion.America);
        AddJob(rules.AsiaChannel, ServerRegion.America);
        AddJob(rules.SARChannel, ServerRegion.America);
    }

    [AutomaticRetry(Attempts = 0)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task UpdateChannelAsync(ulong guildId, ulong channelId, ServerRegion region)
    {
        var channel = await TryGetChannelAsync(guildId, channelId, region);
        if (channel is null) return;

        await TrackRegionAsync(channel, region);
    }

    [AutomaticRetry(Attempts = 0)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public async Task UpdateMessageAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        var message = await TryGetMessageAsync(guildId, channelId, messageId);

        if (message?.Channel is not IGuildChannel channel) return;

        var embed = new EmbedBuilder()
            .WithTitle("Server Status")
            .WithGuildAsAuthor(channel.Guild, AuthorOptions.UseFooter)
            .WithCurrentTimestamp();

        AddRegion(embed, ServerRegion.America, "md");
        AddRegion(embed, ServerRegion.Europe, string.Empty);
        AddRegion(embed, ServerRegion.Asia, "cs");
        AddRegion(embed, ServerRegion.SAR, "fix");

        await message.ModifyAsync(m =>
        {
            m.Content = string.Empty;
            m.Embed   = embed.Build();
        }, RequestOptions);
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

    private async Task RemoveTrackingAsync(ulong guildId)
    {
        var guild = await db.Guilds.FindByIdAsync(guildId);
        if (guild?.GenshinRules?.ServerStatus is null) return;

        RecurringJob.RemoveIfExists(guild.GenshinRules.ServerStatus.Id.ToString());

        guild.GenshinRules.ServerStatus = null;
        await db.SaveChangesAsync();
    }

    private async Task RemoveTrackingAsync(ulong guildId, ServerRegion region)
    {
        var guild = await db.Guilds.FindByIdAsync(guildId);
        var rules = guild?.GenshinRules;
        if (rules is null) return;

        var tracking = region switch
        {
            ServerRegion.America => rules.AmericaChannel,
            ServerRegion.Europe  => rules.EuropeChannel,
            ServerRegion.Asia    => rules.AsiaChannel,
            ServerRegion.SAR     => rules.SARChannel,
            _                    => throw new ArgumentOutOfRangeException(nameof(region), region, null)
        };

        if (tracking is null) return;
        RecurringJob.RemoveIfExists(tracking.Id.ToString());

        db.Remove(tracking);
        await db.SaveChangesAsync();
    }

    private async Task TrackRegionAsync(IGuildChannel channel, ServerRegion region)
    {
        try
        {
            var (name, offset) = ServerOffsets[region];
            await channel.ModifyAsync(c => c.Name = $"{name}: {GetTime(offset)}", RequestOptions);
        }
        catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.Forbidden)
        {
            await RemoveTrackingAsync(channel.Guild.Id, region);
        }
    }

    private async Task<IGuild?> GetGuildAsync(ulong guildId)
    {
        try
        {
            return client.GetGuild(guildId) as IGuild ?? await client.Rest.GetGuildAsync(guildId);
        }
        catch (HttpException e) when (e.DiscordCode is DiscordErrorCode.MissingPermissions)
        {
            await RemoveTrackingAsync(guildId);
            await RemoveTrackingAsync(guildId, ServerRegion.America);
            await RemoveTrackingAsync(guildId, ServerRegion.Europe);
            await RemoveTrackingAsync(guildId, ServerRegion.Asia);
            await RemoveTrackingAsync(guildId, ServerRegion.SAR);

            return null;
        }
    }

    private async Task<IGuildChannel?> TryGetChannelAsync(ulong guildId, ulong channelId, ServerRegion region)
    {
        var guild = await GetGuildAsync(guildId);
        if (guild is null) return null;

        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null)
        {
            await RemoveTrackingAsync(guildId, region);
            return null;
        }

        return channel;
    }

    private async Task<IUserMessage?> TryGetMessageAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        var guild = await GetGuildAsync(guildId);
        if (guild is null) return null;

        var channel = await guild.GetTextChannelAsync(channelId);
        if (channel is null)
        {
            await RemoveTrackingAsync(guildId);
            return null;
        }

        if (await channel.GetMessageAsync(messageId) is not IUserMessage message)
        {
            await RemoveTrackingAsync(guildId);
            return null;
        }

        return message;
    }

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