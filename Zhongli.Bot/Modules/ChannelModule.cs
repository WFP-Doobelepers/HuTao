using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Zhongli.Bot.Modules;

[Group("channel")]
[Name("Channel Management")]
[Summary("Manages channels and sync permissions.")]
[RequireBotPermission(GuildPermission.ManageChannels)]
[RequireUserPermission(GuildPermission.ManageChannels, Group = nameof(RoleModule))]
// [RequireAuthorization(AuthorizationScope., Group = nameof(RoleModule))]
// Todo; AuthorizationScope.Channels

public class ChannelModule : ModuleBase<SocketCommandContext>
{
    public ChannelModule()
    {
        // Initialize the module here.
    }

    [Command("create")]
    [Summary("Creates a new channel.")]
    public async Task CreateChannelAsync(string name)
    {
        await Context.Guild.CreateTextChannelAsync(name);
    }
}