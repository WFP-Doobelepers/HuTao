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
        Up,
        Down
    }

    [Command("category")]
    [Summary("Returning category channel positions.")]
    public async Task CategoryOfChannel(INestedChannel givenChannel)
    {
        var positions = await GetCategoryChannelPositionsAsync(givenChannel.CategoryId);
        await ReplyAsync(embed: new EmbedBuilder()
            .WithTitle("Category Channel Positions")
            .AddItemsIntoFields("Positions", positions,
                p => $"{p.Key}: {p.Value} (Actual: {p.Key.Position})").Build());
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
        await ReplyAsync(embed: GetChannelProperties(givenChannel)
            .WithAuthor(Context.User)
            .WithTitle("Deleted Channel")
            .WithDescription(
                "Disintegrating the channel from Discord databases... " +
                "Don't power off your Discord...")
            .WithColor(Color.Red).Build());
        await givenChannel.DeleteAsync();
    }

    [Command("position")]
    [Summary("Gets the position of a channel.")]
    public Task GetChannelPositionAsync(INestedChannel givenChannel) =>
        ReplyAsync(embed: GetChannelProperties(givenChannel)
            .WithAuthor(Context.User)
            .WithTitle("Position Of Channel").Build());

    [Command("move")]
    [Summary("Move a channel position in a specific direction by a given number.")]
    public async Task PositionMoveChannel(
        [Summary("The channel to move.")] INestedChannel givenChannel,
        [Summary("The direction to move the channel in.")]
        MovementDirection direction,
        [Summary("The amount to move it by")] uint givenNumber)
    {
        if (givenNumber is 0)
            return;

        var moveBy = direction switch
        {
            MovementDirection.Up   => -givenNumber,
            MovementDirection.Down => givenNumber,
            _ => throw new ArgumentOutOfRangeException(
                nameof(direction), direction, "Invalid direction.")
        };

        await SwapChannelPositionsAsync(givenChannel, moveBy);
        await ReplyAsync(embed: GetChannelProperties(givenChannel)
            .WithAuthor(Context.User)
            .WithTitle("Channel move")
            .WithDescription(
                $"Moved channel \"{givenChannel}\" " +
                $"{moveBy} {direction.ToString().ToLower()} positions.")
            .WithColor(Color.Green).Build());
    }

    [Command("movedown")]
    [Summary("Move a channel position downward.")]
    public async Task PositionMoveDownChannel(INestedChannel givenChannel)
    {
        await SwapChannelPositionsAsync(givenChannel, 1);
        await ReplyAsync(embed: GetChannelProperties(givenChannel)
            .WithAuthor(Context.User)
            .WithTitle("Channel move")
            .WithDescription($"The channel \"{givenChannel}\" has been moved downward.")
            .WithColor(Color.Green).Build());
    }

    [Command("moveup")]
    [Summary("Move a channel position upward.")]
    public async Task PositionMoveUpChannel(INestedChannel givenChannel)
    {
        await SwapChannelPositionsAsync(givenChannel, -1);
        await ReplyAsync(embed: GetChannelProperties(givenChannel)
            .WithAuthor(Context.User)
            .WithTitle("Chanel move")
            .WithDescription($"The channel \"{givenChannel}\" has been moved upward.")
            .WithColor(Color.Green).Build());
    }

    [Command("reset")]
    [Summary("Resetting category channel positions.")]
    public async Task ResetCategoryOfChannel([Summary(
            "The channel to reset the positions of its parent category. " +
            "If it has no parent category, it will reset the positions of channels without one.")]
        INestedChannel givenChannel)
    {
        try
        {
            var positions = await ResetCategoryChannelAsync(givenChannel.CategoryId);
            await ReplyAsync(embed: GetChannelProperties(givenChannel)
                .WithAuthor(Context.User)
                .WithTitle("Reset Category")
                .WithDescription(givenChannel.CategoryId is null
                    ? "Channels with no category have been reset"
                    : $"Channels in the category <#{givenChannel.CategoryId}> have been reset.")
                .AddItemsIntoFields("Positions", positions,
                    p => $"{p.Key}: {p.Value} (Actual: {p.Key.Position})")
                .WithColor(Color.Green).Build());
        }
        catch (HttpException e)
        {
            await ReplyAsync(embed: GetChannelProperties(givenChannel)
                .WithAuthor(Context.User).WithTitle("Reset Category")
                .WithDescription(new StringBuilder()
                    .AppendLine($"An error occurred while resetting the category <#{givenChannel.CategoryId}>.")
                    .AppendLine(e.Message).ToString())
                .WithColor(Color.Red).Build());
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

    private EmbedBuilder GetChannelProperties(INestedChannel channel) => new EmbedBuilder()
        .WithUserAsAuthor(Context.User, AuthorOptions.Requested | AuthorOptions.UseFooter)
        .WithTitle($"Channel: {channel.Name}")
        .AddField("Channel ID", channel.Id, true)
        .AddField("Mention", $"<#{channel.Id}>", true)
        .AddField("Position", channel.Position, true)
        .AddField("Category", channel.CategoryId is null ? "None" : $"<#{channel.CategoryId}>", true);

    /// <summary>
    ///     Swaps two channels and their positions based on its index by an offset.
    /// </summary>
    /// <param name="givenChannel">existing channel as INestedChannel type</param>
    /// <param name="by">A value to move the channel by that amount of positions.</param>
    private async Task SwapChannelPositionsAsync(INestedChannel givenChannel, long by)
    {
        var positions = await GetCategoryChannelPositionsAsync(givenChannel.CategoryId);
        var channelPosition = positions.First(c => c.Key.Id == givenChannel.Id).Value;

        var swapPosition = channelPosition + by;
        var min = positions.Min(p => p.Value);
        var max = positions.Max(p => p.Value);

        if (swapPosition < min)
            throw new InvalidOperationException("Channel is already at the top of the category.");

        if (swapPosition > max)
            throw new InvalidOperationException("Channel is already at the bottom of the category.");

        var swapChannel = positions.First(p => p.Value == swapPosition).Key;

        var givenChannelPosition = swapChannel.Position;
        var swapChannelPosition = givenChannel.Position;
        await givenChannel.ModifyAsync(c => { c.Position = givenChannelPosition; });
        await swapChannel.ModifyAsync(c => { c.Position  = swapChannelPosition; });
    }

    /// <summary>
    ///     Based on the given category ID, this method will return a dictionary containing channels and positions.
    /// </summary>
    /// <param name="categoryId">The ID of the category channel, <see langword="null" /> if no category.</param>
    /// <returns>Returns a dictionary containing channels names and positions</returns>
    private async Task<Dictionary<INestedChannel, int>> GetCategoryChannelPositionsAsync(ulong? categoryId)
    {
        var restGuild = await Context.Client.Rest.GetGuildAsync(Context.Guild.Id);
        var restChannels = await restGuild.GetChannelsAsync();
        return restChannels.OfType<INestedChannel>()
            .Where(c => c.CategoryId == categoryId)
            .OrderBy(c => c is IVoiceChannel).ThenBy(c => c.Position)
            .Select((channel, index) => (Channel: channel, Index: index))
            .ToDictionary(g => g.Channel, g => g.Index);
    }

    /// <summary>
    ///     Resetting all positions within the given category ID to the fetched lists index position.
    /// </summary>
    /// <param name="categoryId">The ID of the category channel, <see langword="null" /> if no category.</param>
    /// <returns>Returns a dictionary containing channels names and positions</returns>
    private async Task<Dictionary<INestedChannel, int>> ResetCategoryChannelAsync(ulong? categoryId)
    {
        var category = await GetCategoryChannelPositionsAsync(categoryId);
        foreach (var (channel, index) in category)
        {
            await channel.ModifyAsync(c => { c.Position = index; });
        }

        return await GetCategoryChannelPositionsAsync(categoryId);
    }

    [NamedArgumentType]
    public class ChannelCreationOptions
    {
        [HelpSummary("Create a channel in a specific category.")]
        public ICategoryChannel? ChannelCategory { get; set; }
    }
}