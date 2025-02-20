using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;
using static HuTao.Data.Models.Authorization.AuthorizationScope;

namespace HuTao.Bot.Modules;

[Summary("Commands to view a user's details.")]
public class UserModule(UserService user) : ModuleBase<SocketCommandContext>
{
    [Command("avatar")]
    [Alias("av")]
    [Summary("Get the avatar of the user. Leave empty to view your own avatar.")]
    [RequireAuthorization(User)]
    public Task AvatarAsync([Summary("The mention, username or ID of the user.")] IUser? user1 = null)
        => user.ReplyAvatarAsync(Context, user1 ?? Context.User);

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Summary("View a specific history of a user's infractions.")]
    public Task InfractionsAsync(
        [Summary("The user to show the infractions of.")]
        IUser? user1 = null,
        [Summary("Leave empty to show warnings.")]
        LogReprimandType type = LogReprimandType.None,
        [CheckCategory(History)] ModerationCategory? category = null)
        => user.ReplyHistoryAsync(Context, category, type, user1 ?? Context.User, false);

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Summary("View a specific history of a user's infractions.")]
    [RequireAuthorization(History)]
    public Task InfractionsAsync(
        [Summary("The user to show the infractions of.")]
        IUser? user = null,
        [CheckCategory(History)] ModerationCategory? category = null)
        => InfractionsAsync(user, LogReprimandType.None, category);

    [Command("user")]
    [Alias("whois")]
    [Summary("Views the information of a user. Leave blank to view self.")]
    [RequireAuthorization(User)]
    public Task UserAsync([Summary("The mention, username or ID of the user.")] IUser? user1 = null)
        => user.ReplyUserAsync(Context, user1 ?? Context.User);
}