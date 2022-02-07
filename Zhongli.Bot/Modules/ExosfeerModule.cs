using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Evaluation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules
{
    public class ExosfeerModule: ModuleBase<SocketCommandContext>
    {
        //private bool? ticketStatus = false;

        private string ticketTarget = "Attempts to channel management features, mostly just reordering a channel and then syncing channels easily as well as some sort of way to apply permissions to multiple channels quickly.";
/*
        [Command("moveChannelCI")]
        [Summary("Attempts to channel management features, mostly just reordering a channel and then syncing channels easily as well as some sort of way to apply permissions to multiple channels quickly ")]
        public async Task ExosfeerFirstTicketCIAsync(INestedChannel givenChannel, string categoryName)
        {
            var channelName = givenChannel.Name.ToString();
            var embed = new EmbedBuilder()
                .WithTitle("Attempting to fix ticket !")
                .WithUserAsAuthor(Context.User, AuthorOptions.Requested)
                .AddField("Ticket target: ", $"{ticketTarget}");

            //check if the given channel is an active channel
            try
            {

                embed.AddField("Executing: ", $"channel check").WithCurrentTimestamp();
                if (givenChannel == null)
                {
                    embed.WithDescription("This channel is not an active channel, please try again.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                else
                {
                    embed.WithDescription("This channel is an active channel, moving on.");
                    await ReplyAsync("", false, embed.Build());

                    embed.AddField("Executing: ", $"category check").WithCurrentTimestamp();

                    //check if the given category is an active
                    //get category by category name
                    var category = Context.Guild.CategoryChannels.FirstOrDefault(x => x.Name == categoryName);
                    Task.Delay(TimeSpan.FromSeconds(3));
                    if(category == null)
                    {
                        embed.WithDescription("This category is not an active category, please try again.");
                        await ReplyAsync("", false, embed.Build());
                    }
                    else
                    {
                        embed.WithDescription($"Trying to move {givenChannel.Name.ToString()} to the category {categoryName}");
                        await ReplyAsync("", false, embed.Build());
                    }
                }
            }
            catch (System.Exception e)
            {
                embed.WithDescription("An error occured while trying to verify the channel name, please try again later.");
                await ReplyAsync("", false, embed.Build());
            }

            var message = await ReplyAsync(embed: embed.Build());

            await message.ModifyAsync(m => m.Embeds = new[] { embed.Build() });
        }
*/
        [Command("moveChannel")]
        [Summary("Attempts to channel management features, mostly just reordering a channel and then syncing channels easily as well as some sort of way to apply permissions to multiple channels quickly ")]
        public async Task ExosfeerFirstTicketAsync(INestedChannel givenChannel, ICategoryChannel category)
        {
            var channelName = givenChannel.Name.ToString();
            var channelCheck = (givenChannel != null);
            var categoryCheck = (category != null);
            var embed = new EmbedBuilder()
                .WithTitle("Attempting to fix ticket !")
                .WithUserAsAuthor(Context.User, AuthorOptions.Requested)
                .AddField("Ticket target: ", $"{ticketTarget}");

            //check if the given channel is an active channel
            try
            {

                if (channelCheck)
                {
                    embed.AddField("Channel Check: ", "This channel is an active channel, moving on.").WithCurrentTimestamp();
                    //check if the given category is an active
                    //get category by category name
                    if (categoryCheck)
                    {
                        embed.AddField("Category Check: ", $"Trying to move {givenChannel?.Name.ToString()} to the category {category?.Name.ToString()}").WithCurrentTimestamp();
                    }
                    else
                    {
                        embed.AddField("Category Check: ", "This category is not an active category, please try again.").WithCurrentTimestamp();
                    }
                }
                else
                {
                    embed.AddField("Channel Check: ", "This channel is not an active channel, please try again.").WithCurrentTimestamp();
                }
            }
            catch (System.Exception e)
            {
                embed.WithDescription($"An error occured while trying to verify the channel name, please try again later. Error: \"{e}\"");
            }
            embed.AddField("Channel Check: ", $"{channelCheck}.").WithCurrentTimestamp();
            embed.AddField("Category Check: ", $"{categoryCheck}.").WithCurrentTimestamp();

            var message = await ReplyAsync(embed: embed.Build());

            await message.ModifyAsync(m => m.Embeds = new[] { embed.Build() });
        }

    }
}
