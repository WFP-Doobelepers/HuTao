using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace HuTao.Bot.Modules.Logging;

[Name("Logging Configuration")]
[Group("log")]
[Alias("logs", "logging")]
[Summary("Logging configuration.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class LoggingModule : ModuleBase<SocketCommandContext>
{
    public enum LoggingChannelContext
    {
        Moderator,
        Public
    }

    public enum LoggingContext
    {
        Command,
        User,
        Moderator,
        Public
    }

    private readonly HuTaoContext _db;

    public LoggingModule(HuTaoContext db) { _db = db; }

    [Command("appeal")]
    [Summary("Show the appeal message on a reprimand type.")]
    public async Task ConfigureAppealAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingContext context,
        [Summary("The type of reprimand to show the appeal message on. Leave blank to view current settings.")]
        LogReprimandType type = LogReprimandType.None,
        [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
        bool? showAppeal = null)
    {
        var config = await GetConfigAsync(context);
        if (type is not LogReprimandType.None)
        {
            var reprimands = config.ShowAppealOnReprimands.SetValue(type, showAppeal);
            config.ShowAppealOnReprimands = reprimands;
            await _db.SaveChangesAsync();
        }

        await ReplyAsync($"Current value: {config.ShowAppealOnReprimands.Humanize()}");
    }

    [Command("silent")]
    [Summary("Delete the invoked command when doing these reprimands")]
    public async Task ConfigureAppealAsync(
        [Summary("The type of reprimand to change. Leave blank to view current settings.")]
        LogReprimandType type = LogReprimandType.None,
        [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
        bool? isSilent = null)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationLoggingRules;
        if (type is not LogReprimandType.None)
        {
            var reprimands = rules.SilentReprimands.SetValue(type, isSilent);
            rules.SilentReprimands = reprimands;
            await _db.SaveChangesAsync();
        }

        await ReplyAsync($"Current value: {rules.SilentReprimands.Humanize()}");
    }

    [Command("appeal message")]
    [Summary("Set the appeal message when someone is reprimanded.")]
    public async Task ConfigureAppealMessageAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingContext context,
        [Remainder] [Summary("Leave empty to disable the appeal message.")]
        string? message = null)
    {
        var config = await GetConfigAsync(context);
        config.AppealMessage = message;
        await _db.SaveChangesAsync();

        var embed = new EmbedBuilder()
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        if (message is null)
        {
            embed.WithDescription("The appeal message has been successfully removed.")
                .WithColor(Color.LightGrey)
                .WithTitle("Appeal Message Cleared");
        }
        else
        {
            embed.WithDescription($"The appeal message has been set to: {Format.Bold(message)}")
                .WithColor(Color.Green)
                .WithTitle("Appeal Message Set");
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("channel")]
    [Summary("Set the channel to output the reprimand.")]
    public async Task ConfigureChannelAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingChannelContext context,
        [Summary("The type of reprimand to show the appeal message on. Leave blank to view current settings.")]
        ITextChannel channel)
    {
        var config = await GetConfigAsync(context);
        config.ChannelId = channel.Id;
        await _db.SaveChangesAsync();

        await ReplyAsync($"Current value: {config.MentionChannel()}");
    }

    [Command("moderation rules")]
    [Summary("Configure the moderation logging options.")]
    public async Task ConfigureLoggingChannelAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingContext context,
        [Summary("The logging option to configure. Leave blank to view current settings.")]
        ModerationLogOptions type = ModerationLogOptions.None,
        [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
        bool? state = null)
    {
        var config = await GetConfigAsync(context);
        if (type is not ModerationLogOptions.None)
        {
            var options = config.Options.SetValue(type, state);
            config.Options = options;
            await _db.SaveChangesAsync();
        }

        await ReplyAsync($"Current value: {config.Options.Humanize()}");
    }

    [Command("event")]
    [Summary("Enable or disable specific events to be logged.")]
    public async Task ConfigureLoggingChannelAsync(
        [Summary("The type of log event to configure. Comma separated.")]
        IReadOnlyCollection<LogType> types,
        [Summary("Leave empty to disable these events.")]
        ITextChannel? channel = null)
    {
        if (!types.Any()) return;

        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.LoggingRules.LoggingChannels;
        await SetLoggingChannelAsync(channel, types, rules);

        if (channel is not null)
            await ReplyAsync($"Set the log events {types.Humanize()} to be sent to {channel.Mention}.");
        else
            await ReplyAsync($"Disabled the log events {types.Humanize()}.");
    }

    [Command("reprimand")]
    [Summary("Enable or disable specific reprimands to be logged.")]
    public async Task ConfigureLoggingChannelAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingContext context,
        [Summary("The type of reprimand to configure.")]
        LogReprimandType type,
        [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
        bool? state = null)
    {
        var config = await GetConfigAsync(context);
        if (type is not LogReprimandType.None)
        {
            var reprimands = config.LogReprimands.SetValue(type, state);
            config.LogReprimands = reprimands;
            await _db.SaveChangesAsync();
        }

        await ReplyAsync($"Current value: {config.LogReprimands.Humanize()}");
    }

    [Command("status")]
    [Summary("Enable or disable specific reprimand status to be logged.")]
    public async Task ConfigureLoggingChannelAsync(
        [Summary("The context in which the appeal message will show.")]
        LoggingContext context,
        [Summary("The type of reprimand status to configure.")]
        LogReprimandStatus type,
        [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
        bool? state = null)
    {
        var config = await GetConfigAsync(context);
        if (type is not LogReprimandStatus.None)
        {
            var reprimands = config.LogReprimandStatus.SetValue(type, state);
            config.LogReprimandStatus = reprimands;
            await _db.SaveChangesAsync();
        }

        await ReplyAsync($"Current value: {config.LogReprimandStatus.Humanize()}");
    }

    private async Task SetLoggingChannelAsync<T>(
        IChannel? channel, IEnumerable<T> types,
        ICollection<EnumChannel<T>> collection) where T : Enum
    {
        _db.RemoveRange(collection.Where(rule => types.Contains(rule.Type)));

        if (channel is not null)
        {
            foreach (var type in types)
            {
                collection.Add(new EnumChannel<T>(type, channel));
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task<IChannelEntity> GetConfigAsync(LoggingChannelContext context)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationLoggingRules;
        return context switch
        {
            LoggingChannelContext.Moderator => rules.ModeratorLog,
            LoggingChannelContext.Public    => rules.PublicLog,
            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context, "Invalid logging context.")
        };
    }

    private async Task<ModerationLogConfig> GetConfigAsync(LoggingContext context)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationLoggingRules;
        return context switch
        {
            LoggingContext.Command   => rules.CommandLog,
            LoggingContext.User      => rules.UserLog,
            LoggingContext.Moderator => rules.ModeratorLog,
            LoggingContext.Public    => rules.PublicLog,
            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context, "Invalid logging context.")
        };
    }
}