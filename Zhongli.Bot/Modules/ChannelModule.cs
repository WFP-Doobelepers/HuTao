using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Config;
using Zhongli.Services.CommandHelp;

namespace Zhongli.Bot.Modules;

[Group("channel")]
[Name("Channel Management")]
[Summary("Manages channels and sync permissions.")]
[RequireBotPermission(GuildPermission.ManageChannels)]
[RequireUserPermission(GuildPermission.ManageChannels, Group = nameof(ChannelModule))]
// [RequireAuthorization(AuthorizationScope., Group = nameof(RoleModule))]
// Todo; AuthorizationScope.Channels

public class ChannelModule : ModuleBase<SocketCommandContext>
{
    private ICommandHelpService CommandHelpService;
    public ChannelModule(ICommandHelpService CommandHelpService)
    {
        this.CommandHelpService = CommandHelpService;
    }


    /* Create Channel */
    [Command("create")]
    [Summary("Creates a new channel.")]
    public async Task CreateChannelAsync(string name, ChannelCreationOptions? options = null)
    {
        await Context.Guild.CreateTextChannelAsync(name);
    }

    /* Delete Channel */
    [Command("delete")]
    [Summary("Deletes a channel.")]
    public async Task DeleteChannelAsync(INestedChannel? givenChannel)
    {
        //write to console
        await Context.Channel.SendMessageAsync("Deleting channel...");
        Console.WriteLine("Given Channel: \""+givenChannel+"\"");//loggging TODO: remove later.
        if (givenChannel is not null)
        {
            await givenChannel.DeleteAsync();
        }
        else
        {
            CommandHelpService.TryGetEmbed("channel", HelpDataType.Module, out var paginated);
        }
    }

    /* Sync Permissions */
    public async Task SyncPermissionsAsync(INestedChannel? givenChannel)
    {
        if (givenChannel is not null)
        {
            await givenChannel.SyncPermissionsAsync();
        }
    }




    /* Reorder Channel Order */
    //@param INestedChannel channelName
    //@param int channelPosition = default 1
    //@param IChannelCategory channelCategory

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }


}