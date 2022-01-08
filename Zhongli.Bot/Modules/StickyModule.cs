using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Services.Core.Preconditions.Commands;

namespace Zhongli.Bot.Modules;

public class StickyModule : ModuleBase<SocketCommandContext>
{
    [Command("stick")]
    [Alias("sticky")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task StickyAsync(IMessage message)
    {
        var sticky = new StickyMessage(message);
    }
}