<<<<<<< HEAD
﻿using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
=======
﻿using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
>>>>>>> ae844ea (Added coop-request command and interactions)

namespace HuTao.Bot.Modules;

[Name("CoopRequests")]
[Summary("Deals with Coop requests.")]

public class CoopRequestsModule : ModuleBase<SocketCommandContext>
{
<<<<<<< HEAD
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
=======
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
>>>>>>> ae844ea (Added coop-request command and interactions)
        if (uid.Success)
        {
            return uid.Value;
        }
        else
        {
            return null;
        }
    }

<<<<<<< HEAD
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

=======


    }

>>>>>>> ae844ea (Added coop-request command and interactions)
