using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules;

public class GeneralModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Shows the ping latency of the bot.")]
    public async Task PingAsync()
    {
        var sw = new Stopwatch();
        sw.Start();

        var latency = Context.Client.Latency.Milliseconds().Humanize(5);
        var embed = new EmbedBuilder()
            .WithTitle("Pong!")
            .WithUserAsAuthor(Context.User, AuthorOptions.Requested)
            .AddField("Gateway Latency", $"{latency}")
            .WithCurrentTimestamp();

        var message = await ReplyAsync(embed: embed.Build());

        sw.Stop();
        embed.AddField("Discord Latency", sw.Elapsed.Humanize(5));

        await message.ModifyAsync(m => m.Embeds = new[] { embed.Build() });
    }
}