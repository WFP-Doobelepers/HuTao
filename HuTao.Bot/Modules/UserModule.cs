using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Moderation;
using static HuTao.Data.Models.Authorization.AuthorizationScope;

namespace HuTao.Bot.Modules;

[Discord.Commands.Summary("Commands to view a user's details.")]
public class UserModule(UserService userService) : ModuleBase<SocketCommandContext>
{
    [Command("avatar")]
    [Alias("av")]
    [Discord.Commands.Summary("Get the avatar of the user. Leave empty to view your own avatar.")]
    [RequireAuthorization(User)]
    public Task AvatarAsync([Discord.Commands.Summary("The mention, username or ID of the user.")] IUser? user = null)
        => userService.ReplyAvatarAsync(Context, user ?? Context.User);

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Discord.Commands.Summary("View a specific history of a user's infractions.")]
    public Task InfractionsAsync(
        [Discord.Commands.Summary("The user to show the infractions of.")]
        IUser? user = null,
        [Discord.Commands.Summary("Leave empty to show warnings.")]
        LogReprimandType type = LogReprimandType.None,
        [CheckCategory(History)] ModerationCategory? category = null)
        => userService.ReplyHistoryAsync(Context, category, type, user ?? Context.User, false);

    [Command("history")]
    [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
    [Discord.Commands.Summary("View a specific history of a user's infractions.")]
    [RequireAuthorization(History)]
    public Task InfractionsAsync(
        [Discord.Commands.Summary("The user to show the infractions of.")]
        IUser? user = null,
        [CheckCategory(History)] ModerationCategory? category = null)
        => InfractionsAsync(user, LogReprimandType.None, category);

    [Command("user")]
    [Alias("whois")]
    [Discord.Commands.Summary("Views the information of a user. Leave blank to view self.")]
    [RequireAuthorization(User)]
    public Task UserAsync([Discord.Commands.Summary("The mention, username or ID of the user.")] IUser? user = null)
        => userService.ReplyUserAsync(Context, user ?? Context.User);
}