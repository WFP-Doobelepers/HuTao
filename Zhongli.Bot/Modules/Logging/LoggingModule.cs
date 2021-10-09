using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Logging
{
    [Name("Logging Configuration")]
    [Group("log")]
    [Alias("logs", "logging")]
    [Summary("Logging configuration.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class LoggingModule : ModuleBase<SocketCommandContext>
    {
        private readonly ZhongliContext _db;

        public LoggingModule(ZhongliContext db) { _db = db; }

        [Command("appeal")]
        [Summary("Show the appeal message on a reprimand type.")]
        public async Task ConfigureAppealAsync(
            [Summary("The type of reprimand to show the appeal message on. Leave blank to view current settings.")]
            ReprimandNoticeType type = ReprimandNoticeType.None,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? showAppeal = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            if (type is not ReprimandNoticeType.None)
            {
                var reprimands = guild.LoggingRules.ShowAppealOnReprimands.SetValue(type, showAppeal);
                guild.LoggingRules.ShowAppealOnReprimands = reprimands;
                await _db.SaveChangesAsync();
            }

            await ReplyAsync($"Current value: {guild.LoggingRules.ShowAppealOnReprimands.Humanize()}");
        }

        [Command("appeal message")]
        [Summary("Set the appeal message when someone is reprimanded.")]
        public async Task ConfigureAppealMessageAsync(
            [Remainder] [Summary("Leave empty to disable the appeal message.")]
            string? message = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.LoggingRules.ReprimandAppealMessage = message;

            await _db.SaveChangesAsync();

            var embed = new EmbedBuilder()
                .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            if (message is null)
            {
                embed.WithDescription("The appeal message has been succesfully removed.")
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

        [Command("moderation rules")]
        [Summary("Configure the moderation logging options.")]
        public async Task ConfigureLoggingChannelAsync(
            [Summary("The logging option to configure. Leave blank to view current settings.")]
            ReprimandOptions type = ReprimandOptions.None,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? state = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            if (type is not ReprimandOptions.None)
            {
                var options = guild.ModerationRules.Options.SetValue(type, state);
                guild.ModerationRules.Options = options;
                await _db.SaveChangesAsync();
            }

            await ReplyAsync($"Current value: {guild.ModerationRules.Options.Humanize()}");
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

            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
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
            [Summary("Set to 'null' to disable these events.")]
            ITextChannel? channel,
            [Summary("The type of log event to configure. Space separated.")]
            params ReprimandType[] types)
        {
            if (!types.Any()) return;

            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            var rules = guild.ModerationRules.LoggingChannels;
            await SetLoggingChannelAsync(channel, types, rules);

            if (channel is not null)
                await ReplyAsync($"Set the log events {types.Humanize()} to be sent to {channel.Mention}.");
            else
                await ReplyAsync($"Disabled the log events {types.Humanize()}.");
        }

        [Command("notify")]
        [Summary("Notify user on a reprimand type.")]
        public async Task ConfigureNotificationAsync(
            [Summary("The type of reprimand to notify on. Leave blank to view current settings.")]
            ReprimandNoticeType type = ReprimandNoticeType.None,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? notifyUser = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            if (type is not ReprimandNoticeType.None)
            {
                var notify = guild.LoggingRules.NotifyReprimands.SetValue(type, notifyUser);
                guild.LoggingRules.NotifyReprimands = notify;
                await _db.SaveChangesAsync();
            }

            await ReplyAsync($"Current value: {guild.LoggingRules.NotifyReprimands.Humanize()}");
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
    }
}