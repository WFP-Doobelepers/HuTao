using System;
using System.Threading.Tasks;
using Discord.Interactions;
using HuTao.Services.Linking;

namespace HuTao.Bot.Modules.Linking;

public class InteractiveMessagesModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LinkingService _linking;

    public InteractiveMessagesModule(LinkingService linking) { _linking = linking; }

    [ComponentInteraction("linked:*")]
    public async Task ViewTemplateAsync(string id) => await _linking.SendMessageAsync(Context, new Guid(id));
}