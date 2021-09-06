using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("logging")]
    [Name("Logging Configuration")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ConfigureLoggingModule : ModuleBase<SocketCommandContext>
    {
        private readonly ZhongliContext _db;

        public ConfigureLoggingModule(ZhongliContext db) { _db = db; }

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
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("moderation rules")]
        [Summary("Configure the moderation logging options.")]
        public async Task ConfigureLoggingRulesAsync(
            [Summary("The logging option to configure. Leave blank to view current settings.")]
            LoggingOptions type = LoggingOptions.None,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? state = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            if (type is not LoggingOptions.None)
            {
                var options = guild.LoggingRules.Options.SetValue(type, state);
                guild.LoggingRules.Options = options;
                await _db.SaveChangesAsync();
            }

            await ReplyAsync($"Current value: {guild.LoggingRules.Options.Humanize()}");
        }

        [Command("message channel")]
        [Summary("Configures the Logging Channel that logs for messages will be sent on.")]
        public async Task ConfigureMessageChannelAsync(
            [Summary("Mention, ID, or name of the text channel that the logs will be sent.")]
            ITextChannel channel)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.MessageLogChannelId = channel.Id;

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("moderation channel")]
        [Summary("Configures the Logging Channel that logs will be sent on.")]
        public async Task ConfigureModerationChannelAsync(
            [Summary("Mention, ID, or name of the text channel that the logs will be sent.")]
            ITextChannel channel)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.ModerationChannelId = channel.Id;

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
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

        [Command("reaction channel")]
        [Summary("Configures the Logging Channel that logs for messages will be sent on.")]
        public async Task ConfigureReactionChannelAsync(
            [Summary("Mention, ID, or name of the text channel that the logs will be sent.")]
            ITextChannel channel)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.ReactionLogChannelId = channel.Id;

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}