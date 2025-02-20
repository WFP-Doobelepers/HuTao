using System;
using System.Threading.Tasks;
using Discord.Interactions;
using HuTao.Services.Linking;

namespace HuTao.Bot.Modules.Linking;

public class InteractiveMessagesModule(LinkingService linking) : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("linked:*")]
    public async Task ViewTemplateAsync(string id) => await linking.SendMessageAsync(Context, new Guid(id));
}