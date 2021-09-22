using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Hangfire;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.TimeTracking;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.TimeTracking;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("time")]
    [Name("Time")]
    [Summary("Commands which show when an event in Genshin happens.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class TimeTrackingModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandErrorHandler _error;
        private readonly GenshinTimeTrackingService _time;
        private readonly ZhongliContext _db;

        public TimeTrackingModule(CommandErrorHandler error, GenshinTimeTrackingService time, ZhongliContext db)
        {
            _error = error;
            _time  = time;
            _db    = db;
        }

        [Command("genshin")]
        [Summary("Creates an embed in a channel with the weekly reset time for all servers.")]
        public async Task GenshinAsync(ITextChannel channel)
        {
            var message = await channel.SendMessageAsync("Setting up...");
            await GenshinAsync(message);
        }

        [Command("genshin")]
        [Summary("Changes a message to an embed with the weekly reset time for all servers.")]
        public async Task GenshinAsync(IUserMessage? message = null)
        {
            message ??= await ReplyAsync("Setting up...");

            if (message.Channel is SocketGuildChannel channel
                && channel.Guild.Id != Context.Guild.Id)
            {
                await _error.AssociateError(Context.Message, "Invalid message.");
                return;
            }

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.GenshinRules ??= new GenshinTimeTrackingRules();

            var serverStatus = guild.GenshinRules.ServerStatus;
            if (serverStatus is not null)
            {
                var removed = _db.Remove(serverStatus).Entity;
                RecurringJob.RemoveIfExists(removed.Id.ToString());
            }

            guild.GenshinRules.ServerStatus = new MessageTimeTracking
            {
                GuildId   = guild.Id,
                ChannelId = message.Channel.Id,
                MessageId = message.Id
            };

            await _db.SaveChangesAsync();
            _time.TrackGenshinTime(guild.GenshinRules);

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}