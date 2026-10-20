using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
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

        // The output can hold secrets such as the token, so it goes to the invoker's DMs and never to the channel.
        var embed = EvaluationService.BuildEmbed(context, result);
        try
        {
            await Context.User.SendMessageAsync(
                components: embed.Build().ToComponentsV2Message(),
                allowedMentions: AllowedMentions.None);

            if (Context.Guild is not null)
                await ReplyAsync("Sent the result to your DMs.", allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.CannotSendMessageToUser)
        {
            await ReplyAsync("Could not DM the result, so it was not posted. Open your DMs and run it again.",
                allowedMentions: AllowedMentions.None);
        }
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