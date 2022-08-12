using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using HuTao.Data;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Interactive.Paginator;

namespace HuTao.Services.Interactive;

public abstract class InteractiveEntity<T> : InteractivePromptBase where T : class
{
    protected const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";

    public CommandErrorHandler Error { get; init; } = null!;

    public HuTaoContext Db { get; init; } = null!;

    protected int ChunkSize => 6;

    protected abstract EmbedBuilder EntityViewer(T entity);

    protected abstract string Id(T entity);

    protected async Task AddEntitiesAsync(ICollection<T> entities)
    {
        var collection = await GetCollectionAsync();
        foreach (var entity in entities)
        {
            collection.Add(entity);
        }

        await Db.SaveChangesAsync();
        await Context.Message.AddReactionAsync(new Emoji("✅"));
        await PagedViewAsync(entities);
    }

    protected Task AddEntitiesAsync(params T[] entities) => AddEntitiesAsync((ICollection<T>) entities);

    // protected async Task AddEntitiesAsync(T entity)
    // {
    //     var collection = await GetCollectionAsync();
    //     collection.Add(entity);
    //
    //     await Db.SaveChangesAsync();
    //     await Context.Message.AddReactionAsync(new Emoji("✅"));
    //     await ReplyAsync(embed: EntityViewer(entity)
    //         .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
    //         .WithColor(Color.Green).WithCurrentTimestamp().Build());
    // }

    protected async Task PagedViewAsync(IEnumerable<T> collection) => await PagedViewAsync(collection, EntityViewer);

    protected async Task PagedViewAsync<TEntity>(IEnumerable<TEntity> collection,
        Func<TEntity, EmbedBuilder> entityViewer)
    {
        var pages = collection
            .Chunk(ChunkSize)
            .Select(fields =>
            {
                var builders = fields.Select(entityViewer);
                return new MultiEmbedPageBuilder().WithBuilders(builders);
            });

        var paginated = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages);
        await Service.SendPaginatorAsync(paginated.WithUsers(Context.User).Build(), Context.Channel);
    }

    protected virtual async Task RemoveEntityAsync(string id)
    {
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);

        if (entity is null)
        {
            await Error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        await RemoveEntityAsync(entity);
        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    protected virtual async Task RemoveEntityAsync(T entity)
    {
        Db.Remove(entity);
        await Db.SaveChangesAsync();
    }

    protected virtual async Task ViewEntityAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    protected abstract Task<ICollection<T>> GetCollectionAsync();

    protected async Task<T?> TryFindEntityAsync(string id, IEnumerable<T>? collection = null)
        => await Service.TryFindEntityAsync(Context, collection ?? await GetCollectionAsync(), EntityViewer, Id, id);
}