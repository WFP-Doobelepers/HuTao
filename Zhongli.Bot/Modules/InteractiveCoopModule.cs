using Discord.Interactions;
using Discord;

using System.Threading.Tasks;
using Discord.Commands;

namespace Zhongli.Bot.Modules;

public class InteractiveCoopModule : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("help")]
    public async Task HelpButtonHandler()
    {
        await RespondAsync(
            $"{Context.Interaction.User.Mention} has taken up this request on this button\nPlease follow the Co-op rules for an enjoyable and safe playing experience.");


    }
}