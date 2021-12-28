using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Mapster.Utils;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules;

public class InteractiveUserModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly GenericBitwise<LogReprimandType> InfractionTypeBitwise = new();

    private readonly UserService _user;

    public InteractiveUserModule(UserService user) { _user = user; }

    [UserCommand("Show Avatar")]
    [SlashCommand("avatar", "Get the avatar of the user.")]
    public async Task AvatarAsync([Summary(description: "The user to show")] IUser user)
        => await _user.ReplyAvatarAsync(Context, user);

    [SlashCommand("history", "View a history of a user's infractions")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task HistoryAsync(
        [Summary(description: "The user to show history of")]
        IUser user,
        [Summary(description: "Leave empty to show warnings")]
        LogReprimandType type = LogReprimandType.Warning)
        => await _user.ReplyHistoryAsync(Context, type, user, false);

    [UserCommand("User Information")]
    [SlashCommand("user", "Views the information of a user")]
    public async Task UserAsync([Summary(description: "The user to show")] IUser user)
        => await _user.ReplyUserAsync(Context, user);

    [UserCommand("Reprimand History")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public Task UserHistoryAsync(IUser user) => HistoryAsync(user);

    [ComponentInteraction("r:*")]
    public async Task ViewHistoryAsync(string id, string[] selections)
    {
        var user = await Context.Client.Rest.GetUserAsync(ulong.Parse(id));
        var types = InfractionTypeBitwise.Or(selections.Select(Enum<LogReprimandType>.Parse));

        await _user.ReplyHistoryAsync(Context, types, user, true);
    }
}