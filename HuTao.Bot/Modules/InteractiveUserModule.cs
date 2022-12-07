using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Authorization.AuthorizationScope;

namespace HuTao.Bot.Modules;

public class InteractiveUserModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly GenericBitwise<LogReprimandType> InfractionTypeBitwise = new();
    private readonly UserService _user;

    public InteractiveUserModule(UserService user) { _user = user; }

    [SlashCommand("avatar", "Get the avatar of the user.")]
    [RequireAuthorization(User)]
    public async Task SlashAvatarAsync(
        [Summary(description: "The user to show")] IUser user,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = false)
        => await _user.ReplyAvatarAsync(Context, user, ephemeral);

    [SlashCommand("history", "View a history of a user's infractions")]
    public async Task SlashHistoryAsync(
        [Summary(description: "The user to show history of")] IUser user,
        [Summary(description: "Leave empty to show warnings and notices")]
        LogReprimandType type = LogReprimandType.None,
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(History)]
        ModerationCategory? category = null,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = false)
        => await _user.ReplyHistoryAsync(Context, category, type, user, false, ephemeral);

    [SlashCommand("user", "Views the information of a user")]
    [RequireAuthorization(User)]
    public async Task SlashInformationAsync(
        [Summary(description: "The user to show")] IUser user,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = false)
        => await _user.ReplyUserAsync(Context, user, ephemeral);

    [SlashCommand("whois", "Views the information of a user")]
    [RequireAuthorization(User)]
    public async Task SlashWhoIsAsync(
        [Summary(description: "The user to show")] IUser user,
        [Summary(description: "False to let other users see the message")]
        bool ephemeral = false)
        => await _user.ReplyUserAsync(Context, user, ephemeral);

    [UserCommand("Show Avatar")]
    [RequireAuthorization(User)]
    public Task UserAvatarAsync(IUser user) => SlashAvatarAsync(user, true);

    [UserCommand("Reprimand History")]
    [RequireAuthorization(History, Group = nameof(History))]
    [RequireCategoryAuthorization(History, Group = nameof(History))]
    public Task UserHistoryAsync(IUser user) => SlashHistoryAsync(user, ephemeral: true);

    [UserCommand("User Mod Menu")]
    [RequireAuthorization(User)]
    public Task UserInformationAsync(IUser user) => SlashInformationAsync(user, true);

    [ComponentInteraction("history:*")]
    [RequireAuthorization(History, Group = nameof(History))]
    [RequireCategoryAuthorization(History, Group = nameof(History))]
    public Task ComponentHistoryAsync(IUser user) => SlashHistoryAsync(user);

    [ComponentInteraction("user:*")]
    [RequireAuthorization(User)]
    public Task ComponentInformationAsync(IUser user) => SlashInformationAsync(user);

    [ComponentInteraction("reprimand:*:*")]
    public Task ComponentReprimandsAsync(string id,
        [CheckCategory(History)] ModerationCategory? category, LogReprimandType[] types)
        => ComponentReprimandsAsync(
            id, InfractionTypeBitwise.Or(types),
            new[] { category ?? ModerationCategory.None });

    [ComponentInteraction("category:*:*")]
    public async Task ComponentReprimandsAsync(string id, LogReprimandType type,
        [CheckCategory(History)] ModerationCategory[] categories)
    {
        var category = categories.FirstOrDefault();
        var user = await Context.Client.Rest.GetUserAsync(ulong.Parse(id));
        await _user.ReplyHistoryAsync(Context, category, type, user, true);
    }
}