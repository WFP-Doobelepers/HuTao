using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Moderation;

namespace Zhongli.Services.Interactive;

public abstract class InteractiveTrigger<T> : InteractiveEntity<T> where T : Trigger
{
    private readonly CommandErrorHandler _error;
    private readonly ModerationService _moderation;

    protected InteractiveTrigger(CommandErrorHandler error, ZhongliContext db, ModerationService moderation)
        : base(error, db)
    {
        _error      = error;
        _moderation = moderation;
    }

    [Command("delete")]
    [Summary("Deletes a trigger by ID. Associated reprimands will be deleted.")]
    protected async Task DeleteTriggerAsync(string id,
        [Summary("Silently delete the reprimands in case there are too many.")]
        bool silent = false)
    {
        var collection = await GetCollectionAsync();
        var trigger = await TryFindEntityAsync(id, collection);

        if (trigger is null)
        {
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        await _moderation.DeleteTriggerAsync(trigger, (IGuildUser) Context.User, silent);
        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    [Command("enable")]
    [Summary("Enables a trigger by ID.")]
    protected Task EnableEntityAsync(string id) => ToggleEntityAsync(id, true);

    [Command("disable")]
    [Summary("Disables a trigger by ID. Associated reprimands will be kept.")]
    protected override Task RemoveEntityAsync(string id) => ToggleEntityAsync(id, false);

    [Command("toggle")]
    [Summary("Toggles a trigger by ID. Associated reprimands will be kept.")]
    protected async Task ToggleEntityAsync(
        [Summary("The ID of the trigger.")] string id,
        [Summary("Leave empty to toggle the state.")]
        bool? state = null)
    {
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);

        if (entity is null)
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
        else
            await ToggleTriggerAsync(entity, state);
    }

    protected override Task RemoveEntityAsync(T entity) => Task.CompletedTask;

    private async Task ToggleTriggerAsync(T entity, bool? state)
    {
        await _moderation.ToggleTriggerAsync(entity, (IGuildUser) Context.User, state);

        var (title, value) = EntityViewer(entity);
        var embed = new EmbedBuilder().AddField(title, value);

        await ReplyAsync(embed: embed.Build());
    }
}