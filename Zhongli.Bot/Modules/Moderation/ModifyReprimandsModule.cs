using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Interactive;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation;

[Name("Reprimand Modification")]
[Summary("Modification of reprimands. Provide a partial ID with at least the 2 starting characters.")]
[RequireAuthorization(AuthorizationScope.Moderator)]
public class ModifyReprimandsModule : InteractiveEntity<Reprimand>
{
    private readonly CommandErrorHandler _error;
    private readonly ModerationLoggingService _logging;
    private readonly ModerationService _moderation;
    private readonly ZhongliContext _db;

    public ModifyReprimandsModule(
        CommandErrorHandler error, ZhongliContext db,
        ModerationLoggingService logging, ModerationService moderation)
        : base(error, db)
    {
        _error = error;
        _db    = db;

        _logging    = logging;
        _moderation = moderation;
    }

    [Command("hide")]
    [Alias("pardon")]
    [Summary("Hide a reprimand, this would mean they are not counted towards triggers.")]
    public async Task HideReprimandAsync(string id, [Remainder] string? reason = null)
    {
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, _moderation.HideReprimandAsync, reason);
    }

    [Command("update")]
    [Summary("Update a reprimand's reason.")]
    public async Task UpdateReprimandAsync(string id, [Remainder] string? reason = null)
    {
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, _moderation.UpdateReprimandAsync, reason);
    }

    [Command("remove")]
    [Alias("delete", "purgewarn")]
    [Summary("Delete a reprimand, this completely removes the data.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("reprimand history")]
    [Alias("warnlist all")]
    [Summary("Views the entire reprimand history of the server.")]
    protected async Task ViewEntityAsync(
        [Summary("Leave empty to show everything.")]
        LogReprimandType type = LogReprimandType.All)
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection.OfType(type));
    }

    protected override (string Title, StringBuilder Value) EntityViewer(Reprimand r)
        => (r.GetTitle(true), r.GetReprimandDetails());

    protected override bool IsMatch(Reprimand entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override async Task RemoveEntityAsync(Reprimand entity)
        => await ModifyReprimandAsync(entity, _moderation.DeleteReprimandAsync);

    protected override async Task<ICollection<Reprimand>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ReprimandHistory;
    }

    private ReprimandDetails GetDetails(IUser user, string? reason)
        => new(user, Context, reason);

    private async Task ModifyReprimandAsync(Reprimand? reprimand,
        UpdateReprimandDelegate update, string? reason = null)
    {
        if (reprimand is null || reprimand.GuildId != Context.Guild.Id)
        {
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        var user = Context.Client.GetUser(reprimand.UserId);
        var details = GetDetails(user, reason);

        await update(reprimand, details);
    }

    private delegate Task UpdateReprimandDelegate(Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default);
}