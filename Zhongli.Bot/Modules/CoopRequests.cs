using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Zhongli.Bot.Modules;

[Name("CoopRequests")]
[Summary("Deals with Coop requests.")]

public class CoopRequestsModule : ModuleBase<SocketCommandContext>
{
    [Command("req")]
    [Summary("Make a request co-op request to other players")]

    public async Task RequestAsync([Remainder] string request)
    {
        var region = Region(request);
        var uid = Uid(request);

        var embed = new EmbedBuilder()
            .WithTitle("Co-op Request")
            .WithThumbnailUrl(Context.Message.Author.GetAvatarUrl())
            .WithDescription(request)
            .AddField($"Region:", inline: true, value: region)
            .AddField("UID:", value: uid, true)
            .WithFooter($"Requested by {Context.Message.Author.Username}")
            .WithCurrentTimestamp();

        var button = new ComponentBuilder()
            .WithButton("Help", "help");

        await ReplyAsync("", false, components: button.Build(), embed: embed.Build());


    }
    public string Region(string message)
    {
        message = message.ToLower();
        var region = "";

        if (message.Contains("asia"))
        {
            return region = "Asia";
        }
        else if (message.Contains("europe"))
        {
            return region = "Europe";
        }
        else if (message.Contains("na") || message.Contains("north america") || message.Contains("america"))
        {
            return region = "North America";
        }
        else
        {
            return region = "Unspecified";
        }
    }
    public static string Uid(string text)
    {
        string Uid = @"\(?\d{9}\)?";

        Match uid = Regex.Match(text,Uid);
        if (uid.Success)
        {
            return uid.Value;
        }
        else
        {
            return null;
        }
    }



    }

