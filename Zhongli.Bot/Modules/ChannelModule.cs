using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions.Commands;
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
        // TODO: Add channel creation options.

        var channel = await Context.Guild.CreateTextChannelAsync(name);

        var embed = new EmbedBuilder()
           .WithTitle($"Created Channel \"{name}\" : {channel.Id}")
           .AddField("Channel ID", channel.Id, true)
           // .AddField() TODO: Display channel creation options.
           .WithDescription($"Successfully created channel with name \"{name}.\"")
           .WithAuthor(Context.User);

       await Context.Channel.SendMessageAsync(embed: embed.Build());
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
        await Task.CompletedTask;
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
    [Summary("Synchronizes permissions of a channels within a channel category.")]
    public async Task SyncPermissionsAsync(params INestedChannel[]? givenChannels)
    {
        if (givenChannels is not null && givenChannels.Length > 0)
        {
            var unsyncedChannels = new Dictionary<string, string>();

            await Context.Channel.SendMessageAsync("Attempting to sync permissions of given channels...");
            foreach (var channel in givenChannels)
            {
                try
                {
                    await channel.SyncPermissionsAsync();
                }
                catch (Exception ex)
                {
                    unsyncedChannels.Add(channel.Name, ex.Message);
                }
            }

            var mappedUnsyncedChannels = "";
            foreach (var (channelName, channelException) in unsyncedChannels)
            {
                mappedUnsyncedChannels += $"`\"{channelException}\" : {channelException}`\n";
            }

            var embed = new EmbedBuilder()
                .WithTitle("Channel Permissions Sync Result")
                .WithDescription(unsyncedChannels.Count == 0 ? "Synced all channels successfully!" : $"**{unsyncedChannels.Count} / {givenChannels.Length} Channels failed to sync channel permissions:\n{mappedUnsyncedChannels}**")
                .WithAuthor(Context.User);

            await Context.Channel.SendMessageAsync(embed: embed.Build());
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

    [Command("moveup")]
    [Summary("Move a channel position upward.")]
    public async Task PositionMoveUpChannel(INestedChannel givenChannel)
    {
        if (givenChannel.Id == 0)
        {
            await Context.Channel.SendMessageAsync($"Cannot find channel \"{givenChannel}\".");
            return;
        }

        int currentPosition = givenChannel.Position;
        int updatedPosition = 0;

        if (currentPosition == 0)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is already at the top of all channels.");
            return;
        }
        //Swap channel with channel above it (upward swap are negatives values)
        SwapChannelPositions(givenChannel, -1);

        if (updatedPosition == currentPosition)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has not been moved upward. It is at the top of its category.");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has been moved upward.");
        }
    }

    [Command("movedown")]
    [Summary("Move a channel position downward.")]
    public async Task PositionMoveDownChannel(INestedChannel givenChannel)
    {
        if (givenChannel.Id == 0)
        {
            await Context.Channel.SendMessageAsync($"Cannot find channel \"{givenChannel}\".");
            return;
        }

        int currentPosition = givenChannel.Position;
        int updatedPosition = 0;

        if (currentPosition == Context.Guild.Channels.Count - 1)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" is already at the bottom of all channels.");
            return;
        }

        //Swap channel with channel above it (downward swap are positive values)
        SwapChannelPositions(givenChannel, 1);

        if (updatedPosition == currentPosition)
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has not been moved downward. It is at the bottom of its category.");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"The channel \"{givenChannel}\" has been moved downward.");
        }
    }

    [Command("move")]
    [Summary("Move a channel position downward.")]
    public async Task PositionMoveDownChannel(INestedChannel givenChannel, string direction, int givenNumber = 1)
    {
        //Swap channel with channel above it (upward downward swap are positive values)
        var moveBy = (direction.ToLower() == "up") ? -givenNumber : givenNumber;
        SwapChannelPositions(givenChannel, moveBy);
    }

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }

    /*** IGNORE SKELETON CODE ***/

    //moveup channel
    //[channel position of boths to be swapped]
    //solution 1: swap positioin method with async delay security.(slight problem, position could not be read during async callm. Also prerequisite of a category reset.)
    //solution 2: after move up, renumber all current channels to occurring index. (slight problem, not sure we can get the channels in order. Also move up channel, )
    /*** IGNORE SKELETON CODE ***/

    //[Command("resetcategory")]
    [Command("reset")]
    [Summary("Resetting category channel positions.")]
    public async Task ResetCategoryOfChannel(INestedChannel givenChannel)
    {
        if (givenChannel.CategoryId is null)
        {
            await Context.Channel.SendMessageAsync($"The channel `{givenChannel}` is not in a category.");
            return;
        }

        var category = Context.Guild.GetCategoryChannel(givenChannel.CategoryId.Value);
        try
        {
            var categoryId = (ulong) givenChannel.CategoryId;
            await ResetCategoryChannels(categoryId);

            await Context.Channel.SendMessageAsync($"Channels in category `{category.Name}` have been reset.");
        }
        catch (Exception e)
        {
            await Context.Channel.SendMessageAsync($"An error occurred while resetting the category `{category.Name}`: \n\t{e.Message}");
        }
    }



    //[Command("getcategory")]
    [Command("category")]
    [Summary("Returning category channel positions.")]
    public async Task CategoryOfChannel(INestedChannel givenChannel)
    {
        await Context.Channel.SendMessageAsync("Attempting to fetch channel positions...");
        try
        {
            var categoryId = (ulong) givenChannel.CategoryId;
            var categoryChannels = await GetCategoryChannels(categoryId);
            var mappedPositions = "";

            foreach (var (channelName, channelPosition) in categoryChannels)
            {
                mappedPositions += $"{channelName} - Index Position: {channelPosition}\n";
            }

            var embed = new EmbedBuilder()
                .WithTitle("Category Channel Positions")
                .WithDescription($"List of Channel Positions:\n{mappedPositions}");

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            await Context.Channel.SendMessageAsync($"Failed to fetch channel positions.\n{e.Message}");
        }
    }


    /**********************************************************************************************************************/
    /*** USEFUL PRIVATE METHODS FOR THIS MODULE ***/

    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<Dictionary<string, int>> ResetCategoryChannels(ulong categoryId)
    {
        SocketCategoryChannel categoryChannel = Context.Guild.GetCategoryChannel(categoryId);
        var returnDict = new Dictionary<string, int>();


        foreach (var (channel, index) in categoryChannel.Channels.Select((value, i) => (value, i)))
        {
            Console.WriteLine($"[{channel.GetType().ToString()}]Channel: {channel.Name} Positon: {channel.Position}");
            await channel
                    .ModifyAsync(gC => {gC.Position = index;})
                    .ContinueWith(async _ =>
                        {
                            await Context.Channel.SendMessageAsync(
                                $"[Category][Position][\"{channel.Position}\"][\"{channel.Name}\"]");
                            returnDict.Add(channel.Name, (int) channel.Position);
                        }
                    );
        }
        return returnDict;
    }

    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<Dictionary<string, int>> GetCategoryChannels(ulong categoryId)
    {
        SocketCategoryChannel categoryChannel = Context.Guild.GetCategoryChannel(categoryId);
        var returnDict = new Dictionary<string, int>();


        foreach (var (channel, index) in categoryChannel.Channels.Select((value, i) => (value, i)))
        {
            Console.WriteLine($"[{channel.GetType().ToString()}]Channel: {channel.Name} Positon: {channel.Position}");
            returnDict.Add(channel.Name, (int) channel.Position);
        }
        return returnDict;
    }

    /**
     * Swapping two channels positions asynchronously and informs the end user.
     */
    private async void SwapChannelPositions(INestedChannel givenChannel, int by)
    {
        var categoryId = (ulong) givenChannel.CategoryId;
        SocketCategoryChannel categoryChannel = Context.Guild.GetCategoryChannel(categoryId);
        int positionGivenChannel = givenChannel.Position;


        foreach (var (channel, index) in categoryChannel.Channels.Select((value, i) => (value, i)))
        {
            if(channel.Position == positionGivenChannel + by)
            {
                int positionTargetChannel = channel.Position;
                await channel
                .ModifyAsync(currentChannel => {currentChannel.Position = positionGivenChannel;})
                .ContinueWith(async _ =>
                    {
                        //set given channel
                        await givenChannel.ModifyAsync(gC => {gC.Position = positionTargetChannel;})
                            .ContinueWith(async __ =>
                            {
                                await Context.Channel.SendMessageAsync(
                                    $"Swapped positions of \"{givenChannel.Name}\" with \"{channel.Name}\"");
                            });
                    }
                );
                break;
            }
        }
    }


    /**
     * Return an array containing positions of all channels in the given category.
     * @param ICategoryChannel channelCategory
     */
    private async Task<Dictionary<string, int>> GetCategoryMinMaxPositions(ulong categoryId)
    {
        int returnMin = 0;
        int returnMax = 0;
        //get all categories
        await GetCategoryChannels(categoryId).ContinueWith(async currentCategories =>
        {
            returnMin = currentCategories.Result.Values.Min();
            returnMax = currentCategories.Result.Values.Max();
        });
        
        //overwrite return values with lowest and highest position
        return new Dictionary<string, int>()
        {
            {"min", returnMin},
            {"max", returnMax}
        };
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