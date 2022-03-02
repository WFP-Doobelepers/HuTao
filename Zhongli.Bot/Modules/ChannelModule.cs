using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions.Commands;
using System.Linq;
using Discord.WebSocket;

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
            try
            {
                await givenChannel.SyncPermissionsAsync();
                await Context.Channel.SendMessageAsync($"Syncing permissions of \"{givenChannel}\"");
            }
            catch (Exception e)
            {
                var errorFeedback = $"_Syncing error: **{e.Message}** _";
                await Context.Channel.SendMessageAsync($"\"{errorFeedback}\"");
            }
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

    /* GetChannelPosition */
    [Command("position")]
    [Summary("Gets the position of a channel.")]
    public async Task GetChannelPositionAsync(INestedChannel? givenChannel)
    {
        if (givenChannel is not null)
        {
            await Context.Channel.SendMessageAsync($"The position of \"{givenChannel}\" is {givenChannel.Position}");
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
        if (givenChannel.CategoryId is not null)
        {
            var channelCategoryId = (ulong) givenChannel.CategoryId;
            var initialPosition = (int) givenChannel.Position;
            var categoryChannels = await GetCategoryChannels(channelCategoryId);
            //var allCategories = await GetAllCategories(channelCategoryId);
            var categoryMinPosition = categoryChannels.Min(x => x.Value);
            var categoryMaxPosition = categoryChannels.Max(x=>x.Value);
            Console.WriteLine($"Current position_A: {initialPosition}, Min: {categoryMinPosition}, Max: {categoryMaxPosition}, moving by: {moveBy}");
            if(initialPosition == categoryMinPosition)
            {
                await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is already at the top of the list.");
                return;
            }
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is being moved.. This might take a while..");
            await givenChannel.ModifyAsync(gC =>
            {
                if ((initialPosition - moveBy) > categoryMinPosition &&
                    (initialPosition - moveBy) < categoryMaxPosition)
                {
                    gC.Position = initialPosition - 1;
                    moveBy--;
                }
            }).ContinueWith(async t =>
            {
                Console.WriteLine($"Current position_B: {initialPosition}, Min: {categoryMinPosition}, Max: {categoryMaxPosition}, moving by: {moveBy}");
                 if (moveBy > 0 &&
                     (givenChannel.Position - moveBy) >= categoryMinPosition &&
                     (initialPosition - moveBy) <= categoryMaxPosition)
                 {
                     //await PositionMoveUpChannel(givenChannel, moveBy);//recursive
                 }
                await Context.Channel.SendMessageAsync(
                    $"Moved the channel \"{givenChannel}\" from position \"{initialPosition}\" to position \"{givenChannel.Position}\".");
            });


            Console.WriteLine($"Current position_C: {initialPosition}, Min: {categoryMinPosition}, Max: {categoryMaxPosition}, moving by: {moveBy}");        }
    }

    [Command("movedown")]
    [Summary("Move a channel position downward.")]
    public async Task PositionMoveDownChannel(INestedChannel givenChannel)
    {
        int currentPosition = (int) givenChannel.Position;
        var updatedPosition = 0;

        if (currentPosition == Context.Guild.Channels.Count - 1)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is already at the bottom of the list.");
            return;
        }


        await givenChannel.ModifyAsync(gC =>
        {
            gC.Position     = currentPosition + 1;
            updatedPosition = gC.Position.Value;
        });

        if (updatedPosition == currentPosition)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has not been moved downward. It is at the bottom of its category.");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has been moved downward.");
        }
    }

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }

    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<Dictionary<string, int>> GetCategoryChannels(ulong categoryId, bool reset = false)
    {
        SocketCategoryChannel categoryChannel = Context.Guild.GetCategoryChannel(categoryId);
        var returnDict = new Dictionary<string, int>();
        foreach (var (channel, index) in categoryChannel.Channels.Select((value, i) => (value, i)))
        {
            Console.WriteLine($"[{channel.GetType().ToString()}]Channel: {channel.Name} Positon: {channel.Position}");
            if (reset){await channel.ModifyAsync(gC =>
            {
                gC.Position = index;
            }).ContinueWith(async t =>
            {
                await Context.Channel.SendMessageAsync(
                    $"[Category][Position][\"{channel.Position}\"][\"{channel.Name}\"]");
                returnDict.Add(channel.Name, (int) channel.Position);
            });}
            else
            {
                await Context.Channel.SendMessageAsync(
                    $"[Category][Position][\"{channel.Position}\"][\"{channel.Name}\"]");
                returnDict.Add(channel.Name, (int) channel.Position);

            }
        }
        return returnDict;
    }

    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<Dictionary<string, int>> GetCategoryMinMaxPositions(ICategoryChannel categoryId)
    {
        //TODO: implement later
        throw new NotImplementedException();
    }

    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<ICategoryChannel[]> GetAllCategories(ulong categoryId)
    {
        var returnCategories = new ICategoryChannel[0];
        ICategoryChannel serverChannels = Context.Guild.GetCategoryChannel(categoryId);
        //get all channels from one specific category


        IReadOnlyCollection<IGuildChannel> channels = await serverChannels.Guild.GetChannelsAsync();
        foreach(var channel in channels) {
            if(channel.Id == categoryId) {
                var categoryChannels = await channel.Guild.GetCategoriesAsync(CacheMode.AllowDownload);
                foreach (var categoryChannel in categoryChannels)
                {
                    if (categoryChannel.GetType().ToString().ToLower().Contains("category"))
                    {
                        Console.WriteLine($"[{categoryChannel.GetType().ToString()}]Channel: {categoryChannel.Name} Positon: {categoryChannel.Position}");
                    }
                    else
                    {

                        Console.WriteLine($"[{categoryChannel.GetType().ToString()}]Channel: {categoryChannel.Name} Positon: {categoryChannel.Position}");
                    }

                }
            }
        }
        return returnCategories;
    }
}