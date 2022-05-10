using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;

namespace HuTao.Bot.Modules;

[Summary("Commands to view a user's details.")]
public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly UserService _user;

    public UserModule(UserService user) { _user = user; }

    [Command("avatar")]
    [Alias("av")]
    [Summary("Get the avatar of the user. Leave empty to view your own avatar.")]
    [RequireAuthorization(AuthorizationScope.User)]
    public async Task AvatarAsync(
        [Summary("The mention, username or ID of the user.")] IUser? user = null)
    {
        user ??= Context.User;
        await _user.ReplyAvatarAsync(Context, user);
    }

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Summary("View a specific history of a user's infractions.")]
    [RequireAuthorization(AuthorizationScope.History)]
    public async Task InfractionsAsync(
        [Summary("The user to show the infractions of.")] IUser? user = null,
        [Summary("Leave empty to show warnings.")] LogReprimandType type
            = LogReprimandType.Warning | LogReprimandType.Notice,
        ModerationCategory? category = null)
    {
        user ??= Context.User;
        await _user.ReplyHistoryAsync(Context, category, type, user, false);
    }

    [Command("user")]
    [Alias("whois")]
    [Summary("Views the information of a user. Leave blank to view self.")]
    [RequireAuthorization(AuthorizationScope.User)]
    public async Task UserAsync(
        [Summary("The mention, username or ID of the user.")] IUser? user = null)
    {
        user ??= Context.User;
        await _user.ReplyUserAsync(Context, user);
    }
}