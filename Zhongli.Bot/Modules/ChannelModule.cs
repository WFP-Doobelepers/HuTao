using System;
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
    private readonly ICommandHelpService CommandHelpService;
    public ChannelModule(ICommandHelpService commandHelpService)
    {
        this.CommandHelpService = commandHelpService;
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
        await Context.Channel.SendMessageAsync("Disintegrating the channel from Discord databases... Don't power off your Discord..");
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
    [Command("sync")]
    [Summary("Synchronizes permissions of a specific channel to it's channel Category.")]
    public async Task SyncPermissionsAsync(INestedChannel? givenChannel)
    {
        Console.WriteLine("Syncing permissions...");
        if (givenChannel is not null)
        {
            await givenChannel.SyncPermissionsAsync();
            await Context.Channel.SendMessageAsync($"Syncing permissions of \"{givenChannel}\"");
        }
    }

    /* Sync Permissions */
    [Command("sync category")]
    [Summary("Synchronizes permissions of a channels wiithin a channel category.")]
    public async Task SyncPermissionsAsync(params INestedChannel[]? givenChannels)
    {
        if (givenChannels is not null && givenChannels.Length > 0)
        {
            foreach (var channel in givenChannels)
            {
                await channel.SyncPermissionsAsync();
            }
        }
    }

    /* Reorder Channel Order */
    //@param INestedChannel channelName
    //@param int channelPosition = default 1
    //@param IChannelCategory channelCategory

    [Command("moveup")]
    [Summary("Move a channel position upward.")]
    public async Task PositionMoveUpChannel(INestedChannel givenChannel, int moveBy = 1)
    {
        int currentPosition = (int) givenChannel.Position;
        Console.WriteLine($"Current position_A: {currentPosition}");
        if(currentPosition == 1)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is already at the top of the list.");
            return;
        }
        await givenChannel.ModifyAsync(gC =>
        {
            gC.Position = currentPosition - moveBy;
            moveBy--;
        }).ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                await Context.Channel.SendMessageAsync($"Failed to move the channel \"{givenChannel}\" up by {moveBy} positions.");
            }
            else
            {
                if (moveBy > 0)
                {
                    await PositionMoveUpChannel(givenChannel, moveBy);
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"Moved the channel \"{givenChannel}\" up by {moveBy} positions.");
                }
            }
        });


        await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is being moved.. This might take a while..");
        Console.WriteLine($"Current position_B: {currentPosition}");
    }

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }


}