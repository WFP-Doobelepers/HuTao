using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Hangfire;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.TimeTracking;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.TimeTracking;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Configuration;

[Name("Time Tracking")]
[Group("time")]
[Summary("Time tracking module.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class TimeTrackingModule : ModuleBase<SocketCommandContext>
{
    private readonly CommandErrorHandler _error;
    private readonly GenshinTimeTrackingService _time;
    private readonly HuTaoContext _db;

    public TimeTrackingModule(CommandErrorHandler error, GenshinTimeTrackingService time, HuTaoContext db)
    {
        _error = error;
        _time  = time;
        _db    = db;
    }

    [Command("genshin")]
    public async Task GenshinAsync(ITextChannel channel)
    {
        var message = await channel.SendMessageAsync("Setting up...");
        await GenshinAsync(message);
    }

    [Command("genshin")]
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
        await _time.TrackGenshinTime(guild);

        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }
}