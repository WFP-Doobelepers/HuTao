using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HuTao.Bot.Modules;

[Name("CoopRequests")]
[Summary("Deals with Coop requests.")]

public class CoopRequestsModule : ModuleBase<SocketCommandContext>
{
    [Command("coop")]
    [Alias("request","req")]
    [Summary("Make a co-op request to other players")]

    public async Task RequestAsync([Remainder] string request)
    {
        await Context.Message.DeleteAsync();

        var uid = Uid(request);
        var region = Region(uid);

        if (uid == "none" || region == "none")
        {
            await ReplyAsync("Please Enter a valid UID.");
        }
        else
        {
            var embed = new EmbedBuilder()
                .WithTitle("Co-op Request")
                .WithThumbnailUrl(Context.Message.Author.GetAvatarUrl())
                .WithDescription(request)
                .AddField($"Region:", region, true)
                .AddField("UID:", uid, true)
                .WithFooter($"Requested by {Context.Message.Author.Username}")
                .WithCurrentTimestamp()
                .Build();

            var buttons = new ComponentBuilder()
                .WithButton("Help", $"help:{Context.User.Id}")
                .WithButton("Close", $"close:{Context.User.Id}", ButtonStyle.Danger)
                .Build();

            await ReplyAsync($"<@&{RolePing(uid)}>", false, components: buttons, embed: embed);
        }
    }

    private static string Uid(string text)
    {
        const string uidPattern = @"\(?\d{9}\)?";
        var uid = Regex.Match(text,uidPattern);

        return uid.Success switch
        {
            true => uid.Value,
            false  => "none"
        };
    }

    private static string Region(string uid)
    {
        var regionId = uid[0] switch
        {
            '6' => "North America",
            '7' => "Europe",
            '8' => "Asia",
            '9' => "SAR",
            _   => "none"
        };
        return regionId;
    }

    private static string RolePing(string uid)
    {
        //Enter the Role ID of the roles to be pinged, namely NA, Europe, Asia, SAR respectively.
        var roleId = uid[0] switch
        {
            '6' => "971378318664429608",
            '7' => "971378290302521394",
            '8' => "971378261328298004",
            '9' => "971378342739734619",
        };
        return roleId;
    }
}

