using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("configure logging")]
    public class LoggingConfigurationModule : ModuleBase
    {
        private static readonly GenericBitwise<LoggingOptions> LoggingOptionsBitwise = new();
        private readonly ZhongliContext _db;

        public LoggingConfigurationModule(ZhongliContext db) { _db = db; }

        [Command]
        [Summary("Configures the Logging Channel that logs will be sent on.")]
        public async Task ConfigureLoggingAsync(
            [Summary("Mention, ID, or name of the text channel that the logs will be sent.")]
            ITextChannel channel)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.ModerationChannelId = channel.Id;

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("verbose")]
        [Summary("Set to 'true' to log secondary reprimands in separate messages.")]
        public async Task VerboseAsync(
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? isVerbose = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.Options
                = LoggingOptionsBitwise.SetValue(guild.LoggingRules.Options, LoggingOptions.Verbose, isVerbose);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("silent")]
        [Summary("Silently reprimand the user and delete the command.")]
        public async Task SilentAsync(
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? isSilent = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.Options
                = LoggingOptionsBitwise.SetValue(guild.LoggingRules.Options, LoggingOptions.Silent, isSilent);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("notifyUser")]
        [Summary("Notify and DM the user about their reprimand.")]
        public async Task NotifyUserAsync(
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? shouldNotifyUser = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.Options
                = LoggingOptionsBitwise.SetValue(guild.LoggingRules.Options, LoggingOptions.NotifyUser, shouldNotifyUser);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("anonymous")]
        [Summary("When the user is notified, make the moderator anonymous.")]
        public async Task AnonymousAsync(
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? isAnonymous = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.Options
                = LoggingOptionsBitwise.SetValue(guild.LoggingRules.Options, LoggingOptions.Anonymous, isAnonymous);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}