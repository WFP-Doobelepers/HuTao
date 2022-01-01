using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using Zhongli.Services.Core.Preconditions.Interactions;
using Zhongli.Services.Evaluation;
using InteractionContext = Zhongli.Data.Models.Discord.InteractionContext;

namespace Zhongli.Bot.Modules;

public class InteractiveGeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EvaluationService _evaluation;

    public InteractiveGeneralModule(EvaluationService evaluation) { _evaluation = evaluation; }

    [SlashCommand("eval", "Evaluate C# code")]
    [RequireTeamMember]
    public async Task EvalAsync([Remainder] string code)
    {
        await DeferAsync(true);
        var context = new InteractionContext(Context);
        var result = await _evaluation.EvaluateAsync(context, code);

        var embed = EvaluationService.BuildEmbed(context, result);
        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}