using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Humanizer;
using HuTao.Data;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Interactive.Paginator;

namespace HuTao.Services.Interactive;

public abstract class InteractiveEntity<T> : InteractivePromptBase where T : class
{
    protected const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;

    protected InteractiveEntity(CommandErrorHandler error, HuTaoContext db)
    {
        _error = error;
        _db    = db;
    }

    protected abstract EmbedBuilder EntityViewer(T entity);

    protected abstract string Id(T entity);

    protected async Task AddEntityAsync(T entity)
    {
        var collection = await GetCollectionAsync();
        collection.Add(entity);

        await _db.SaveChangesAsync();
        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    protected async Task PagedViewAsync(IEnumerable<T> collection) => await PagedViewAsync(collection, EntityViewer);

    protected async Task PagedViewAsync<TEntity>(IEnumerable<TEntity> collection,
        Func<TEntity, EmbedBuilder> entityViewer)
    {
        var pages = collection
            .Chunk(6)
            .Select(fields =>
            {
                var builders = fields.Select(entityViewer);
                return new MultiEmbedPageBuilder().WithBuilders(builders);
            });

        var paginated = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages);
        await Interactive.SendPaginatorAsync(paginated.WithUsers(Context.User).Build(), Context.Channel);
    }

    protected virtual async Task RemoveEntityAsync(string id)
    {
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);

        if (entity is null)
        {
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        await RemoveEntityAsync(entity);
        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    protected virtual async Task RemoveEntityAsync(T entity)
    {
        _db.Remove(entity);
        await _db.SaveChangesAsync();
    }

    protected virtual async Task ViewEntityAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    protected abstract Task<ICollection<T>> GetCollectionAsync();

    protected async Task<T?> TryFindEntityAsync(string id, IEnumerable<T>? collection = null)
    {
        if (id.Length < 2)
            return null;

        collection ??= await GetCollectionAsync();
        var filtered = collection
            .Where(e => IsMatch(e, id))
            .Take(10).ToList();

        if (filtered.Count <= 1)
            return filtered.Count == 1 ? filtered.First() : null;

        var embeds = filtered.Zip(filtered.Select(EntityViewer).Select(e => e.Build())).ToList();
        var options = embeds.Select(f =>
        {
            var embed = f.Second;
            return new SelectMenuOptionBuilder()
                .WithValue(Id(f.First))
                .WithLabel(embed.Title.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength))
                .WithDescription(embed.Description.Truncate(SelectMenuOptionBuilder.MaxDescriptionLength));
        }).ToList();

        var guid = Guid.NewGuid();
        var component = new ComponentBuilder()
            .WithSelectMenu($"select:{guid}", options, "Multiple matches found, select one...")
            .WithButton("Cancel selection", $"cancel:{guid}", ButtonStyle.Danger, new Emoji("🛑"));

        await ReplyAsync(embeds: embeds.Select(e => e.Second).ToArray(), components: component.Build());
        var selected = await Interactive.NextMessageComponentAsync(c
            => c.Data.CustomId == $"select:{guid}"
            || c.Data.CustomId == $"cancel:{guid}");

        _ = selected.Value?.DeferAsync();
        _ = selected.Value?.ModifyOriginalResponseAsync(m => m.Components = new ComponentBuilder().Build());
        return selected.IsSuccess && selected.Value.Data.CustomId == $"select:{guid}"
            ? filtered.FirstOrDefault(e => IsMatch(e, selected.Value.Data.Values.First()))
            : null;

        bool IsMatch(T entity, string match) => Id(entity).StartsWith(match, StringComparison.OrdinalIgnoreCase);
    }
}