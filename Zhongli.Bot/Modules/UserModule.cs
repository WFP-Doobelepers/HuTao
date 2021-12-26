using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;
using CommandContext = Zhongli.Services.Interactive.CommandContext;

namespace Zhongli.Bot.Modules;

[Summary("Commands to view a user's details.")]
public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly UserService _user;

    public UserModule(UserService user) { _user = user; }

    [Command("avatar")]
    [Alias("av")]
    [Summary("Get the avatar of the user. Leave empty to view your own avatar.")]
    public async Task AvatarAsync(
        [Summary("The mention, username or ID of the user.")]
        IUser? user = null)
    {
        user ??= Context.User;

        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId)
            .WithImageUrl(user.GetAvatarUrl(size: 2048))
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Summary("View a specific history of a user's infractions.")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task InfractionsAsync(
        [Summary("The user to show the infractions of.")]
        IUser? user = null,
        [Summary("Leave empty to show warnings.")]
        LogReprimandType type = LogReprimandType.Warning)
    {
        user ??= Context.User;
        await _user.ReplyHistoryAsync(new CommandContext(Context), type, user, false);
    }

    [Command("user")]
    [Alias("whois")]
    [Summary("Views the information of a user. Leave blank to view self.")]
    public async Task UserAsync(
        [Summary("The mention, username or ID of the user.")]
        IUser? user = null)
    {
        user ??= Context.User;
        await _user.ReplyUserAsync(new CommandContext(Context), user);
    }
}