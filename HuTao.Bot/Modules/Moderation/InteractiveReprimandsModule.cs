using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
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
        await ModifyReprimandAsync(reprimand, ephemeral, (r, d, t)
            => _moderation.TryExpireReprimandAsync(r, Pardoned, d, t), reason);
    }

    [SlashCommand("default-category", "Sets the default category for reprimands.")]
    public async Task SetDefaultCategoryAsync(
        [Summary("The category to set as the default.")]
        [Autocomplete(typeof(CategoryAutocomplete))]
        [CheckCategory(History)]
        ModerationCategory? category = null)
    {
        if (category == ModerationCategory.All)
        {
            await RespondAsync("You cannot set the default category to `All`.");
            return;
        }

        var user = await _db.Users.TrackUserAsync(Context.User, Context.Guild);
        user.DefaultCategory = category == ModerationCategory.None ? null : category;
        await _db.SaveChangesAsync();

        await ReplyAsync($"Default reprimand category set to `{user.DefaultCategory?.Name ?? "None"}`.");
    }

    [SlashCommand("update", "Update a reprimand's reason.")]
    public async Task UpdateReprimandAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        string? reason = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var reprimand = await TryFindEntityAsync(id);
        await ModifyReprimandAsync(reprimand, ephemeral, _moderation.UpdateReprimandAsync, reason);
    }

    [SlashCommand("history", "Views the entire reprimand history of the server.")]
    public async Task ViewHistoryAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] [CheckCategory(History)]
        ModerationCategory? category = null,
        LogReprimandType type = LogReprimandType.All,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection.OfType(type)
            .OfCategory(category ?? ModerationCategory.None)
            .OrderByDescending(h => h.Action?.Date));
    }

    [SlashCommand("view", "View the details of the reprimand.")]
    public async Task ViewReprimandAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        bool ephemeral = false)
    {
        var reprimand = await TryFindEntityAsync(id);
        if (reprimand == null)
        {
            await RespondAsync(EmptyMatchMessage, ephemeral: true);
            return;
        }

        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, History, reprimand.Category);
        if (!authorized)
            await RespondAsync(NotAuthorizedMessage, ephemeral: true);
        else
        {
            await RespondAsync(ephemeral: ephemeral,
                embed: reprimand.ToEmbedBuilder(true, EmbedBuilder.MaxDescriptionLength).Build(),
                components: reprimand.ToComponentBuilder(ephemeral).Build());
        }
    }

    [ComponentInteraction("reprimand-delete:*:*", true)]
    [SlashCommand("delete", "Delete a reprimand. This completely removes the data.")]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(ReprimandAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    [ModalInteraction("reprimand-pardon:*:*", true)]
    public Task PardonReprimandAsync(string id, bool ephemeral, PardonModal modal)
        => PardonReprimandAsync(id, modal.Reason, ephemeral);

    [ComponentInteraction("reprimand-pardon:*:*", true)]
    public Task PardonReprimandMenuAsync(string id, bool ephemeral)
        => UpdateReprimandAsync<PardonModal>($"reprimand-pardon:{id}:{ephemeral}", id);

    [ModalInteraction("reprimand-update:*:*", true)]
    public Task UpdateReprimandAsync(string id, bool ephemeral, UpdateModal modal)
        => UpdateReprimandAsync(id, modal.Reason, ephemeral);

    [ComponentInteraction("reprimand-update:*:*", true)]
    public Task UpdateReprimandMenuAsync(string id, bool ephemeral)
        => UpdateReprimandAsync<UpdateModal>($"reprimand-update:{id}:{ephemeral}", id);

    protected override EmbedBuilder EntityViewer(Reprimand entity) => entity.ToEmbedBuilder(true);

    protected override string Id(Reprimand entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(Reprimand entity, bool ephemeral)
    {
        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, Scope, entity.Category);

        if (!authorized)
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
        else
            await ModifyReprimandAsync(entity, ephemeral, _moderation.DeleteReprimandAsync);
    }

    protected override async Task<ICollection<Reprimand>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ReprimandHistory;
    }

    private async Task ModifyReprimandAsync(
        Reprimand? reprimand, bool ephemeral,
        UpdateReprimandDelegate update, string? reason = null)
    {
        var authorized = await _auth.IsCategoryAuthorizedAsync(Context, Scope, reprimand?.Category);

        if (!authorized)
            await FollowupAsync(NotAuthorizedMessage, ephemeral: true);
        else if (reprimand is null || reprimand.GuildId != Context.Guild.Id)
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
        else
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var variables = guild.ModerationRules?.Variables;
            var user = await ((IDiscordClient) Context.Client).GetUserAsync(reprimand.UserId);
            var details = new ReprimandDetails(
                Context, user, reason, variables,
                ephemeral: ephemeral, modify: true);

            await update(reprimand, details);
        }
    }

    private async Task UpdateReprimandAsync<T>(string customId, string id) where T : ReprimandModal
    {
        var reprimand = _db.Set<Reprimand>().FirstOrDefault(r => r.Id.ToString() == id);
        if (reprimand is null)
        {
            await RespondAsync(EmptyMatchMessage, ephemeral: true);
            return;
        }

        var user = await reprimand.GetUserAsync(Context);
        var reason = reprimand.GetLatestReason(TextInputBuilder.LargestMaxLength);
        await Context.Interaction.RespondWithModalAsync<T>(customId, modifyModal: m => m
            .WithTitle($"{m.Title} for {user}".Truncate(45))
            .UpdateTextInput("reason", b => b.WithValue(reason)));
    }

    public class PardonModal : ReprimandModal
    {
        public override string Title => "Pardon Reprimand";
    }

    public abstract class ReprimandModal : IModal
    {
        [RequiredInput(false)]
        [InputLabel("Reason")]
        [ModalTextInput("reason", TextInputStyle.Paragraph, "Reason...")]
        public string? Reason { get; set; }

        public abstract string Title { get; }
    }

    public class UpdateModal : ReprimandModal
    {
        public override string Title => "Update Reprimand";
    }

    private delegate Task UpdateReprimandDelegate(
        Reprimand reprimand, ReprimandDetails details,
        CancellationToken cancellationToken = default);
}