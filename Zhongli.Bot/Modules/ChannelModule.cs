using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules;

[Group("channel")]
[Name("Channel Management")]
[Summary("Manages channels and sync permissions.")]
[RequireBotPermission(GuildPermission.ManageChannels)]
[RequireUserPermission(GuildPermission.ManageChannels, Group = nameof(ChannelModule))]
[RequireAuthorization(AuthorizationScope.Channels, Group = nameof(ChannelModule))]
public class ChannelModule : ModuleBase<SocketCommandContext>
{
    public enum MovementDirection
    {
        Up = -1,
        Down = 1
    }
    [Command("create")]
    [Summary("Creates a new channel.")]
    public async Task CreateChannelAsync(string name, ChannelCreationOptions? options = null)
    {
        // TODO: V2 Add channel creation options.
        var channel = await Context.Guild.CreateTextChannelAsync(name);
        var embed = new EmbedBuilder()
            .WithTitle($"Created Channel \"{name}\" : {channel.Id}")
            .AddField("Channel ID", channel.Id, true)
            // .AddField() TODO: v2 Display channel creation options.
            .WithDescription($"Successfully created channel with name \"{name}.\"")
            .WithAuthor(Context.User);
        await ReplyAsync(embed: embed.Build());
    }
    [Command("delete")]
    [Summary("Deletes a channel.")]
    public async Task DeleteChannelAsync(INestedChannel givenChannel)
    {
        try
        {
            await givenChannel.DeleteAsync();
        }
        catch (HttpException e)
        {
            Console.WriteLine(e);

            var embedError = new EmbedBuilder()
                .WithTitle($"Deleted Channel \"{givenChannel}\" : {givenChannel.Id} HAS FAILED")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription(
                    $"Deleting the channel \"{givenChannel}.\" has failed due to an error. Error: {e.Message}")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embedError.Build());
            throw;
        }
        var embed = new EmbedBuilder()
            .WithTitle($"Deleted Channel \"{givenChannel}\" : {givenChannel.Id}")
            .AddField("Channel ID", givenChannel.Id, true)
            .WithDescription(
                $"Disintegrating the channel from Discord databases... Don't power off your Discord.. \"{givenChannel}.\"")
            .WithAuthor(Context.User);
        await ReplyAsync(embed: embed.Build());
    }
    [Command("sync")]
    [Summary("Synchronizes permissions of a specific channel to it's channel Category.")]
    public async Task SyncPermissionsAsync(INestedChannel givenChannel)
    {
        try
        {
            await givenChannel.SyncPermissionsAsync().ContinueWith(async _ =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Synchronizing Channel \"{givenChannel}\" : {givenChannel.Id}")
                    .AddField("Channel ID", givenChannel.Id, true)
                    .WithDescription($"Synchronizing permissions of \"{givenChannel}\" to it's channel category.")
                    .WithAuthor(Context.User);
                await ReplyAsync(embed: embed.Build());
            });
        }
        catch (HttpException e)
        {
            var errorFeedback = $"_Syncing error: **{e.Message}** _";
            await ReplyAsync($"\"{errorFeedback}\"");
        }
    }
    [Command("sync category")]
    [Summary("Synchronizes permissions of a channels within a channel category.")]
    public async Task SyncCategoryPermissionsAsync(params INestedChannel[]? givenChannels)
    {
        if (givenChannels?.Length > 0)
        {
            var failed = new Dictionary<IChannel, string>();
            foreach (var channel in givenChannels)
            {
                try
                {
                    await channel.SyncPermissionsAsync();
                }
                catch (HttpException ex)
                {
                    failed.Add(channel, ex.Message);
                }
            }
            var embed = new EmbedBuilder()
                .WithDescription("Synced all channels successfully!")
                .WithTitle("Channel Permissions Sync Result")
                .WithAuthor(Context.User);

            if (failed.Any())
            {
                embed
                    .WithDescription($"{failed.Count} / {givenChannels.Length} channels failed to sync.")
                    .AddItemsIntoFields("Failed", failed, (channel, exception) => $"`{channel}` : {exception}`");
            }

            await ReplyAsync(embed: embed.Build());
        }
    }

    [Command("position")]
    [Summary("Gets the position of a channel.")]
    public async Task GetChannelPositionAsync(INestedChannel givenChannel)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Position Of Channel \"{givenChannel}\" : {givenChannel.Id}")
            .AddField("Channel ID", givenChannel.Id, true)
            .WithDescription($"The position of \"{givenChannel}\" is {givenChannel.Position}")
            .WithAuthor(Context.User);
        await ReplyAsync(embed: embed.Build());
    }

    [Command("moveup")]
    [Summary("Move a channel position upward.")]
    public async Task PositionMoveUpChannel(INestedChannel givenChannel)
    {
        var currentPosition = givenChannel.Position;
        if (currentPosition == 0)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"Move Up Channel \"{givenChannel}\" : {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription(
                    $"The channel \"{givenChannel}\" has not been moved upward. It is at the top of the category channels.")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            SwapChannelPositions(givenChannel,
                -1); //Swap channel with channel above it (upward swap are negatives values)
            var embed = new EmbedBuilder()
                .WithTitle($"Move Up Channel \"{givenChannel}\": {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription($"The channel \"{givenChannel}\" has been moved upward.")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
        }
    }

    [Command("movedown")]
    [Summary("Move a channel position downward.")]
    public async Task PositionMoveDownChannel(INestedChannel givenChannel)
    {
        var currentPosition = givenChannel.Position;
        var categoryMinMaxPositions = await GetCategoryMinMaxPositions((ulong) givenChannel.CategoryId);
        Console.WriteLine(categoryMinMaxPositions.Values.Max());
        if (currentPosition == categoryMinMaxPositions.Values.Max())
        {
            SwapChannelPositions(givenChannel,
                -1); //Swap channel with channel above it (upward swap are negatives values)
            var embed = new EmbedBuilder()
                .WithTitle($"Move Down Channel \"{givenChannel}\": {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription(
                    $"The channel \"{givenChannel}\" has not been moved downward. It is at the bottom of the category channels.")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            SwapChannelPositions(givenChannel,
                -1); //Swap channel with channel above it (upward swap are negatives values)
            var embed = new EmbedBuilder()
                .WithTitle($"Move Down Channel \"{givenChannel}\": {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription($"The channel \"{givenChannel}\" has been moved downward.")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
            SwapChannelPositions(givenChannel,
                1); //Swap channel with channel above it (downward swap are positive values)
        }
    }

    [Command("move")]
    [Summary(
        "Move a channel position `up` or `down` and an integer to move more than one. " +
        "Example: `channel move [channel] up 2`")]
    public async Task PositionMoveChannel(INestedChannel givenChannel, MovementDirection direction, int givenNumber = 1)
    {
        var moveBy = 0;
        if (direction is MovementDirection.Up)
            moveBy                                           = -givenNumber;
        else if (direction is MovementDirection.Down) moveBy = givenNumber;
        if (moveBy is not 0)
        {
            var categoryMinMaxPositions
                = await GetCategoryMinMaxPositions((ulong) givenChannel
                    .CategoryId); //Swap channel with channel above it (downward swap are positive values)
            if (moveBy < 0 && moveBy + givenChannel.Position < categoryMinMaxPositions.Values.Min())
            {
                SwapChannelPositions(givenChannel,
                    -1); //Swap channel with channel above it (upward swap are negatives values)
                var embed = new EmbedBuilder()
                    .WithTitle($"Move Channel \"{givenChannel}\": {givenChannel.Id}")
                    .AddField("Channel ID", givenChannel.Id, true)
                    .WithDescription($"The channel \"{givenChannel}\" can't move the channel up to that position.")
                    .WithAuthor(Context.User);
                await ReplyAsync(embed: embed.Build());
            }
            else if (moveBy > 0 && moveBy + givenChannel.Position > categoryMinMaxPositions.Values.Max())
            {
                SwapChannelPositions(givenChannel,
                    -1); //Swap channel with channel above it (upward swap are negatives values)
                var embed = new EmbedBuilder()
                    .WithTitle($"Move Channel \"{givenChannel}\": {givenChannel.Id}")
                    .AddField("Channel ID", givenChannel.Id, true)
                    .WithDescription($"The channel \"{givenChannel}\" can't move the channel down to that position.")
                    .WithAuthor(Context.User);
                await ReplyAsync(embed: embed.Build());
            }
            else
                SwapChannelPositions(givenChannel, moveBy);
        }
    }

    [Command("reset")]
    [Summary("Resetting category channel positions.")]
    public async Task ResetCategoryOfChannel(INestedChannel givenChannel)
    {
        if (givenChannel.CategoryId is null)
        {
            SwapChannelPositions(givenChannel,
                -1); //Swap channel with channel above it (upward swap are negatives values)
            var embed = new EmbedBuilder()
                .WithTitle($"Reset Channel \"{givenChannel}\" : {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription($"The channel `{givenChannel}` is not in a category.")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            var category = Context.Guild.GetCategoryChannel(givenChannel.CategoryId.Value);
            try
            {
                var categoryId = (ulong) givenChannel.CategoryId;
                await ResetCategoryChannels(categoryId);
                var embed = new EmbedBuilder()
                    .WithTitle($"Reset Channel \"{givenChannel}\": {givenChannel.Id}")
                    .AddField("Channel ID", givenChannel.Id, true)
                    .WithDescription($"Channels in category `{category}` have been reset.")
                    .WithAuthor(Context.User);
                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Reset Channel \"{givenChannel}\": {givenChannel.Id}")
                    .AddField("Channel ID", givenChannel.Id, true)
                    .WithDescription(new StringBuilder()
                        .AppendLine($"An error occurred while resetting the category `{category.Name}`")
                        .AppendLine(e.Message).ToString())
                    .WithAuthor(Context.User);
                await ReplyAsync(embed: embed.Build());
            }
        }
    }

    [Command("category")]
    [Summary("Returning category channel positions.")]
    public async Task CategoryOfChannel(INestedChannel givenChannel)
    {
        await ReplyAsync("Attempting to fetch channel positions...");
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

            await ReplyAsync(embed: embed.Build());
        }
        catch (HttpException e)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"Category Channel \"{givenChannel.Name}\" : {givenChannel.Id}")
                .AddField("Channel ID", givenChannel.Id, true)
                .WithDescription($"Failed to fetch channel positions.\n{e.Message}")
                .WithAuthor(Context.User);
            await ReplyAsync(embed: embed.Build());
        }
    }

    /// <summary>
    ///     Resetting all positions within the given category ID to the fetched lists index position.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns>Returns a dictionary containing channels names and positions</returns>
    private async Task<Dictionary<string, int>> ResetCategoryChannels(ulong categoryId)
    {
        var client = Context.Client;
        var socketGuild = Context.Guild;
        var restGuild = await client.Rest.GetGuildAsync(Context.Guild.Id);
        var restChannels = await restGuild.GetChannelsAsync();
        var restDictionary = restChannels.OfType<INestedChannel>()
            .Where(channel => channel.CategoryId != null)
            .GroupBy(c => socketGuild.GetCategoryChannel(c.CategoryId!.Value))
            .OrderBy(c => c.Key.Position)
            .ToDictionary(g => g.Key.Id, g => g.OrderBy(c => c is IVoiceChannel).ThenBy(c => c.Position));
        var returnDict = new Dictionary<string, int>();
        foreach (var (channel, index) in restDictionary[categoryId].Select((value, i) => (value, i)))
        {
            await channel
                .ModifyAsync(gC => { gC.Position = index; })
                .ContinueWith( _ => { returnDict.Add(channel.Name, channel.Position); });
        }
        return returnDict;
    }

    /// <summary>
    ///     Based on the given category ID, this method will return a dictionary containing channel names and positions.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns>Returns a dictionary containing channels names and positions</returns>
    private async Task<Dictionary<string, int>> GetCategoryChannels(ulong categoryId)
    {
        var client = Context.Client;
        var socketGuild = Context.Guild;
        var restGuild = await client.Rest.GetGuildAsync(Context.Guild.Id);
        var restChannels = await restGuild.GetChannelsAsync();
        var restDictionary = restChannels.OfType<INestedChannel>()
            .Where(channel => channel.CategoryId != null)
            .GroupBy(c => socketGuild.GetCategoryChannel(c.CategoryId!.Value))
            .OrderBy(c => c.Key.Position)
            .ToDictionary(g => g.Key.Id, g => g.OrderBy(c => c is IVoiceChannel).ThenBy(c => c.Position));
        var returnDict = new Dictionary<string, int>();
        foreach (var (channel, index) in restDictionary[categoryId].Select((value, i) => (value, i)))
        {
            returnDict.Add(channel.Name, channel.Position);
        }
        return returnDict;
    }

    /// <summary>
    ///     Swapping two channels positions asynchronously.
    /// </summary>
    /// <param name="givenChannel">existing channel as INestedChannel type</param>
    /// <param name="by">integer value that calculates which positioin to swap with.</param>
    /// <returns>No return</returns>
    private async void SwapChannelPositions(INestedChannel givenChannel, int by)
    {
        var categoryId = (ulong) givenChannel.CategoryId;
        var categoryChannel = Context.Guild.GetCategoryChannel(categoryId);
        var positionGivenChannel = givenChannel.Position;

        foreach (var (channel, index) in categoryChannel.Channels.Select((value, i) => (value, i)))
        {
            if (channel.Position == positionGivenChannel + by)
            {
                var positionTargetChannel = channel.Position;
                await channel
                    .ModifyAsync(currentChannel => { currentChannel.Position = positionGivenChannel; })
                    .ContinueWith(async _ =>
                        {
                            await givenChannel.ModifyAsync(gC => { gC.Position = positionTargetChannel; });
                        }
                    );
                break;
            }
        }
    }

    /// <summary>
    ///     Based on a given category ID, return the minimum and maximum position of all channels within the category.
    /// </summary>
    /// <param name="categoryId">existing channel as INestedChannel type</param>
    /// <returns>Return an dictionary containing the minimal and maximum position of an category.</returns>
    private async Task<Dictionary<string, int>> GetCategoryMinMaxPositions(ulong categoryId)
    {
        var currentCategories = await GetCategoryChannels(categoryId);
        return new Dictionary<string, int>
        {
            {"min", currentCategories.Values.Min()},
            {"max", currentCategories.Values.Max()}
        };
    }

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }
}