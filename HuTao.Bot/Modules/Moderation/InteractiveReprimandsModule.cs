using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Authorization.AuthorizationScope;
using static HuTao.Data.Models.Moderation.Infractions.Reprimands.ReprimandStatus;

namespace HuTao.Bot.Modules.Moderation;

[Group("reprimand", "Reprimand management commands")]
public class InteractiveReprimandsModule : InteractionEntity<Reprimand>
{
    private const AuthorizationScope Scope = All | Modify;
    private const string NotAuthorizedMessage = "You are not authorized to modify this reprimand.";
    private readonly AuthorizationService _auth;
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public InteractiveReprimandsModule(HuTaoContext db, AuthorizationService auth, ModerationService moderation)
    {
        _auth       = auth;
        _db         = db;
        _moderation = moderation;
    }

    [SlashCommand("pardon", "Pardon a reprimand, this would mean they are not counted towards triggers.")]
    public async Task PardonReprimandAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        string? reason = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, (r, d, t)
            => _moderation.TryExpireReprimandAsync(r, Pardoned, d, t), reason);
    }

    [SlashCommand("update", "Update a reprimand's reason.")]
    public async Task UpdateReprimandAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        string? reason = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, _moderation.UpdateReprimandAsync, reason);
    }

    [SlashCommand("history", "Views the entire reprimand history of the server.")]
    [RequireAuthorization(History, Group = nameof(History))]
    [RequireCategoryAuthorization(History, Group = nameof(History))]
    public async Task ViewHistoryAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] ModerationCategory? category = null,
        LogReprimandType type = LogReprimandType.All,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection.OfType(type).OfCategory(category).OrderByDescending(h => h.Action?.Date));
    }

    [SlashCommand("delete", "Delete a reprimand. this completely removes the data.")]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(Reprimand entity)
        => entity.ToEmbedBuilder(true);

    protected override string Id(Reprimand entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(Reprimand entity)
    {
        var authorized = await _auth.IsAuthorizedAsync(Context, Scope, entity.Category);

        if (!authorized)
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
        else
            await ModifyReprimandAsync(entity, _moderation.DeleteReprimandAsync);
    }

    protected override async Task<ICollection<Reprimand>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ReprimandHistory;
    }

    private async Task ModifyReprimandAsync(Reprimand? reprimand,
        UpdateReprimandDelegate update, string? reason = null)
    {
        var authorized = await _auth.IsAuthorizedAsync(Context, Scope, reprimand?.Category);

        if (!authorized)
            await FollowupAsync(NotAuthorizedMessage, ephemeral: true);
        else if (reprimand is null || reprimand.GuildId != Context.Guild.Id)
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
        else
        {
            var user = await ((IDiscordClient) Context.Client).GetUserAsync(reprimand.UserId);
            var details = new ReprimandDetails(Context, user, reason);

            await update(reprimand, details);
        }
    }

    private delegate Task UpdateReprimandDelegate(
        Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default);
}