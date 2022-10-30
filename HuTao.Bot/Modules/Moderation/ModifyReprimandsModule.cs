using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Authorization.AuthorizationScope;
using static HuTao.Data.Models.Moderation.Infractions.Reprimands.ReprimandStatus;

namespace HuTao.Bot.Modules.Moderation;

[Name("Reprimand Modification")]
[Summary("Modification of reprimands. Provide a partial ID with at least the 2 starting characters.")]
public class ModifyReprimandsModule : InteractiveEntity<Reprimand>
{
    private const AuthorizationScope Scope = All | Modify;
    private const string NotAuthorizedMessage = "You are not authorized to modify this reprimand.";
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public ModifyReprimandsModule(
        CommandErrorHandler error, HuTaoContext db,
        AuthorizationService auth, ModerationService moderation)
    {
        _auth       = auth;
        _error      = error;
        _db         = db;
        _moderation = moderation;
    }

    [Command("pardon")]
    [Alias("hide")]
    [Summary("Pardon a reprimand, this would mean they are not counted towards triggers.")]
    public async Task PardonReprimandAsync(string id, [Remainder] string? reason = null)
    {
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, (r, d, t)
            => _moderation.TryExpireReprimandAsync(r, Pardoned, d, t), reason);
    }

    [Command("update")]
    [Summary("Update a reprimand's reason.")]
    public async Task UpdateReprimandAsync(string id, [Remainder] string? reason = null)
    {
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, _moderation.UpdateReprimandAsync, reason);
    }

    [Priority(1)]
    [Command("history all")]
    [Alias("reprimand all", "reprimands all")]
    [Summary("Views the entire reprimand history of the server.")]
    public async Task ViewHistoryAsync(
        [CheckCategory(History)] ModerationCategory? category = null,
        [Summary("Leave empty to show everything.")] LogReprimandType type = LogReprimandType.All)
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection.OfType(type)
            .OfCategory(category ?? ModerationCategory.None)
            .OrderByDescending(h => h.Action?.Date));
    }

    [Command("reprimand")]
    [Summary("View the details of the reprimand.")]
    public async Task ViewReprimandAsync(string id)
    {
        var reprimand = await TryFindEntityAsync(id);
        if (reprimand == null)
        {
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, History, reprimand.Category);
        if (!authorized)
            await _error.AssociateError(Context, NotAuthorizedMessage);
        else
        {
            await ReplyAsync(
                embed: reprimand.ToEmbedBuilder(true, EmbedBuilder.MaxDescriptionLength).Build(),
                components: reprimand.ToComponentBuilder().Build());
        }
    }

    [Command("remove")]
    [Alias("delete", "purgewarn")]
    [Summary("Delete a reprimand, this completely removes the data.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    protected override EmbedBuilder EntityViewer(Reprimand entity) => entity.ToEmbedBuilder(true);

    protected override string Id(Reprimand entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(Reprimand entity)
    {
        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, Scope, entity.Category);

        if (!authorized)
            await _error.AssociateError(Context.Message, NotAuthorizedMessage);
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
        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, Scope, reprimand?.Category);

        if (!authorized)
            await _error.AssociateError(Context.Message, NotAuthorizedMessage);
        else if (reprimand is null || reprimand.GuildId != Context.Guild.Id)
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
        else
        {
            var user = await Context.Client.GetUserAsync(reprimand.UserId);
            var details = await GetDetailsAsync(user, reason);

            await update(reprimand, details);
        }
    }

    private async Task<ReprimandDetails> GetDetailsAsync(IUser user, string? reason)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var variables = guild.ModerationRules?.Variables;
        return new ReprimandDetails(Context, user, reason, variables);
    }

    private delegate Task UpdateReprimandDelegate(Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default);
}