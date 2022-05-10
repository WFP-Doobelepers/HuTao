using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Evaluation;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Bot.Modules;

public class InteractiveGeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AuthorizationService _auth;
    private readonly EvaluationService _evaluation;

    public InteractiveGeneralModule(AuthorizationService auth, EvaluationService evaluation)
    {
        _auth       = auth;
        _evaluation = evaluation;
    }

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

    [ComponentInteraction("delete:*:*")]
    public async Task DeleteMessageAsync(ITextChannel channel, ulong messageId)
    {
        if (Context.User is not IGuildUser user)
        {
            await RespondAsync("This only works in a guild.", ephemeral: true);
            return;
        }

        await DeferAsync(true);

        var permissions = user.GetPermissions(channel);
        if (!permissions.ManageMessages &&
            !await _auth.IsAuthorizedAsync(Context, AuthorizationScope.Purge | AuthorizationScope.All))
        {
            await FollowupAsync("You do not have permission to delete messages.", ephemeral: true);
            return;
        }

        var message = await channel.GetMessageAsync(messageId);
        if (message is null)
            await FollowupAsync("The message has already been deleted.", ephemeral: true);
        else
        {
            await message.DeleteAsync();
            await FollowupAsync("Message deleted.", ephemeral: true);
        }

        if (Context.Interaction is IComponentInteraction interaction && interaction.Message.Embeds.Any())
        {
            var embeds = interaction.Message.Embeds.Select(e => e.ToEmbedBuilder().WithColor(Color.Red).Build());
            await interaction.Message.ModifyAsync(m => m.Embeds = embeds.ToArray());
        }
    }
}