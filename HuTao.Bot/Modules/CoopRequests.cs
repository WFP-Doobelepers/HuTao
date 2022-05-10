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

        if (uid is null || region == "Unspecified")
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
        if (uid.Success)
        {
            return uid.Value;
        }
        else
        {
            return null;
        }
    }

    private static string Region(string? uid)
    {
        var regionId = "Unspecified";

        switch (uid[0])
        {
            case '6':
                regionId = "North America";
                break;
            case '7':
                regionId = "Europe";
                break;
            case '8':
                regionId = "Asia";
                break;
            case '9':
                regionId = "SAR";
                break;
        }
        return regionId;
    }

    private static string RolePing(string? uid)
    {
        var roleId = "Unspecified";

        //Enter the Role ID of the roles to be pinged, namely NA, Europe, Asia, SAR respectively.
        switch (uid[0])
        {
            case '6':
                roleId = "952476641165193216";
                break;
            case '7':
                roleId = "952477465828282368";
                break;
            case '8':
                roleId = "952476516158152704";
                break;
            case '9':
                roleId = "952477656228708422";
                break;
        }
        return roleId;
    }
}

