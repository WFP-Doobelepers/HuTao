using System.Linq;
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


    /* Create Channel */
    [Command("create")]
    [Summary("Creates a new channel.")]
    public async Task CreateChannelAsync(string name)
    {
        await Context.Guild.CreateTextChannelAsync(name);
    }

    /* Delete Channel */
    [Command("delete")]
    [Summary("Deletes a channel.")]
    public async Task DeleteChannelAsync(INestedChannel givenChannel)
    {
        //check permissions


        if (givenChannel != null)
        {
            await givenChannel.DeleteAsync();
        }
    }


    /* Reorder Channel Order */
    //@param INestedChannel channelName
    //@param int channelPosition = default 1
    //@param IChannelCategory channelCategory

    /* Sync Permissions */
    // sync permissions for a channel to the channel category group
    //@param channelCategory
    //@param channelName
    /* Manage Channel Permissions*/
}