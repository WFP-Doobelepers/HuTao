using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Evaluation;
using HuTao.Services.Utilities;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Bot.Modules;

public class GeneralModule(EvaluationService evaluation) : ModuleBase<SocketCommandContext>
{
    [Command("eval")]
    [RequireTeamMember]
    public async Task EvalAsync([Remainder] string code)
    {
        var context = new CommandContext(Context);
        var result = await evaluation.EvaluateAsync(context, code);

        var embed = EvaluationService.BuildEmbed(context, result);
        await ReplyAsync(
            components: embed.Build().ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None);
    }

    [Command("ping")]
    [Summary("Shows the ping latency of the bot.")]
    public async Task PingAsync()
    {
        var gateway = Context.Client.Latency.Milliseconds().Humanize(5);

        var initial = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay(
                    "## Pong!\n" +
                    $"**Gateway Latency:** {gateway}\n" +
                    "-# Calculating Discord latency…")
                .WithAccentColor(0x9B59FF))
            .Build();

        var message = await ReplyAsync(components: initial, allowedMentions: AllowedMentions.None);

        var discord = (message.CreatedAt - Context.Message.CreatedAt).Humanize(5);

        var updated = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay(
                    "## Pong!\n" +
                    $"**Gateway Latency:** {gateway}\n" +
                    $"**Discord Latency:** {discord}")
                .WithAccentColor(0x9B59FF))
            .Build();

        await message.ModifyAsync(m => m.Components = updated);
    }
}