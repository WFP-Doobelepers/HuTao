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
using InteractionContext = Zhongli.Services.Interactive.InteractionContext;

namespace Zhongli.Bot.Modules;

public class InteractiveUserModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly GenericBitwise<LogReprimandType> InfractionTypeBitwise = new();

    private readonly UserService _user;

    public InteractiveUserModule(UserService user) { _user = user; }

    [SlashCommand("history", "View a history of a user's infractions")]
    [RequireAuthorization(AuthorizationScope.Moderator)]
    public async Task HistoryAsync(
        [Summary(description: "The user to show history of")]
        IUser user,
        [Summary(description: "Leave empty to show warnings")]
        LogReprimandType type = LogReprimandType.Warning)
        => await _user.ReplyHistoryAsync(new InteractionContext(Context), type, user, false);

    [SlashCommand("user", "Views the information of a user")]
    public async Task UserAsync([Summary(description: "The user to show")] IUser user)
        => await _user.ReplyUserAsync(new InteractionContext(Context), user);

    [ComponentInteraction("r:*")]
    public async Task ViewHistoryAsync(string id, string[] selections)
    {
        var user = await Context.Client.Rest.GetUserAsync(ulong.Parse(id));
        var types = InfractionTypeBitwise.Or(selections.Select(Enum<LogReprimandType>.Parse));

        await _user.ReplyHistoryAsync(new InteractionContext(Context), types, user, true);
    }
}