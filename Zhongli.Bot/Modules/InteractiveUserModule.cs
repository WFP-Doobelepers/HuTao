using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Preconditions.Interactions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;
using static Zhongli.Data.Models.Moderation.Logging.LogReprimandType;

namespace Zhongli.Bot.Modules;

public class InteractiveUserModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly GenericBitwise<LogReprimandType> InfractionTypeBitwise = new();
    private readonly UserService _user;

    public InteractiveUserModule(UserService user) { _user = user; }

    [SlashCommand("avatar", "Get the avatar of the user.")]
    public async Task SlashAvatarAsync(
        [Summary(description: "The user to show")]
        IUser user,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = true)
        => await _user.ReplyAvatarAsync(Context, user, ephemeral);

    [SlashCommand("history", "View a history of a user's infractions")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task SlashHistoryAsync(
        [Summary(description: "The user to show history of")]
        IUser user,
        [Summary(description: "Leave empty to show warnings and notices")]
        LogReprimandType type = Warning | Notice,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = true)
        => await _user.ReplyHistoryAsync(Context, type, user, false, ephemeral);

    [SlashCommand("user", "Views the information of a user")]
    public async Task SlashInformationAsync(
        [Summary(description: "The user to show")]
        IUser user,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = true)
        => await _user.ReplyUserAsync(Context, user, ephemeral);

    [UserCommand("Show Avatar")]
    public Task UserAvatarAsync(IUser user) => SlashAvatarAsync(user);

    [UserCommand("Reprimand History")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public Task UserHistoryAsync(IUser user) => SlashHistoryAsync(user);

    [UserCommand("User Information")]
    public Task UserInformationAsync(IUser user) => SlashInformationAsync(user);

    [ComponentInteraction("history:*")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public Task ComponentHistoryAsync(IUser user) => SlashHistoryAsync(user, ephemeral: false);

    [ComponentInteraction("user:*")]
    public Task ComponentInformationAsync(IUser user) => SlashInformationAsync(user, false);

    [ComponentInteraction("r:*")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task ComponentReprimandsAsync(string id, LogReprimandType[] types)
    {
        var user = await Context.Client.Rest.GetUserAsync(ulong.Parse(id));
        await _user.ReplyHistoryAsync(Context, InfractionTypeBitwise.Or(types), user, true);
    }
}