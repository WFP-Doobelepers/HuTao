using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Evaluation;
using Zhongli.Services.Utilities;
using CommandContext = Zhongli.Data.Models.Discord.CommandContext;

namespace Zhongli.Bot.Modules;

public class GeneralModule : ModuleBase<SocketCommandContext>
{
    private readonly EvaluationService _evaluation;

    public GeneralModule(EvaluationService evaluation) { _evaluation = evaluation; }

    [Command("eval")]
    [RequireTeamMember]
    public async Task EvalAsync([Remainder] string code)
    {
        var context = new CommandContext(Context);
        var result = await _evaluation.EvaluateAsync(context, code);

        var embed = EvaluationService.BuildEmbed(context, result);
        await ReplyAsync(embed: embed.Build());
    }

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