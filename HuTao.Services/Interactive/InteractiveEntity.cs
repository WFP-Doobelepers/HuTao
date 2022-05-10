using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using HuTao.Data;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Interactive.Criteria;
using HuTao.Services.Interactive.Functions;
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

    protected abstract bool IsMatch(T entity, string id);

    protected abstract EmbedBuilder EntityViewer(T entity);

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
            .ToList();

        if (filtered.Count <= 1)
            return filtered.Count == 1 ? filtered.First() : null;

        var containsCriterion = new FuncCriterion(m =>
            int.TryParse(m.Content, out var selection)
            && selection < filtered.Count && selection > -1);

        await PagedViewAsync(filtered);
        var selected = await Interactive.NextMessageAsync(containsCriterion.AsFunc(Context));
        return selected.Value is null ? null : filtered.ElementAtOrDefault(int.Parse(selected.Value.Content));
    }
}