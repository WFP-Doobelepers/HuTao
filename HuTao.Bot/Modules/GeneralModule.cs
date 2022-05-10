using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Evaluation;
using HuTao.Services.Utilities;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Bot.Modules;

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
        var gateway = Context.Client.Latency.Milliseconds().Humanize(5);
        var embed = new EmbedBuilder()
            .WithTitle("Pong!")
            .WithUserAsAuthor(Context.User, AuthorOptions.Requested)
            .AddField("Gateway Latency", $"{gateway}")
            .WithCurrentTimestamp();

        var message = await ReplyAsync(embed: embed.Build());

        var discord = (message.CreatedAt - Context.Message.CreatedAt).Humanize(5);
        embed.AddField("Discord Latency", $"{discord}");

        await message.ModifyAsync(m => m.Embed = embed.Build());
    }
}