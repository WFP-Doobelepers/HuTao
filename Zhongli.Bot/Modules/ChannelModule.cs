using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions.Commands;

namespace Zhongli.Bot.Modules;

[Group("channel")]
[Name("Channel Management")]
[Summary("Manages channels and sync permissions.")]
[RequireBotPermission(GuildPermission.ManageChannels)]
[RequireUserPermission(GuildPermission.ManageChannels, Group = nameof(ChannelModule))]
[RequireAuthorization(AuthorizationScope.Channels, Group = nameof(ChannelModule))]

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
        await Context.Channel.SendMessageAsync("Disintegrating the channel from Discord databases... Powering off.");
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