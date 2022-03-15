using Discord;
using System.Linq;
using Discord.Interactions;
using System.Threading.Tasks;

namespace Zhongli.Bot.Modules;

public class InteractiveCoopModule : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("help:*")]

    public async Task HelpButton(string id )
    {
        if (Context.Interaction is IComponentInteraction interaction)
        {
            if (ulong.Parse(id) != Context.Interaction.User.Id)
            {
                var message = interaction.Message;
                var embeds = message.Embeds.Select(e => e.ToEmbedBuilder()
                    .WithColor(Color.Green)
                    .AddField("Taken by:", $"{interaction.User.Mention}", true)
                    .Build());

                await message.ModifyAsync(m => m.Embeds = embeds.ToArray());

                await RespondAsync(
                    $"{Context.Interaction.User.Mention}Please follow the Co-op rules for an enjoyable and safe playing experience.\nDo Not Re-Click on the Help Button again.",
                    ephemeral: true);
            }
            else
            {
                {
                    await RespondAsync($"Can't help yourself now, Can you? {Context.Interaction.User.Mention}", ephemeral: true);
                }
            }
        }
    }

    [ComponentInteraction("close:*")]

    public async Task CloseButton(string id)
    {
        if (Context.Interaction is IComponentInteraction interaction)
        {
            if (ulong.Parse(id) == Context.Interaction.User.Id)
            {
                var message = interaction.Message;
                var embeds = message.Embeds.Select(e => e.ToEmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Co-op request [CLOSED]")
                    .AddField("Request Closed by", $"{interaction.User.Mention}", true)
                    .Build());

                var newButtons = new ComponentBuilder()
                    .WithButton("Help", disabled: true)
                    .WithButton("Close", disabled: true)
                    .Build();
                await message.ModifyAsync(m => m.Embeds     = embeds.ToArray());
                await message.ModifyAsync(x => x.Components = newButtons);
            }
            else
            {
                await RespondAsync(
                    "Request can only be closed by the requesting individual.\nPlease ping a Moderator if you feel the request is over and should be closed.",
                    ephemeral: true);
            }
        }
    }
}