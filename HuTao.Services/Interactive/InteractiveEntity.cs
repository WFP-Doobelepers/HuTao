using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Services.Interactive;

public abstract class InteractiveEntity<T> : InteractivePromptBase where T : class
{
    protected const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";

    public CommandErrorHandler Error { get; init; } = null!;

    public HuTaoContext Db { get; init; } = null!;

    protected virtual int ChunkSize => 6;

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

    protected async Task PagedViewAsync<TEntity>(
        IEnumerable<TEntity> collection,
        Func<TEntity, EmbedBuilder> entityViewer)
    {
        var items = collection.ToList();
        if (items.Count == 0)
        {
            await ReplyAsync("No results found.");
            return;
        }

        var chunkSize = ChunkSize;
        var pageCount = Math.Max(1, (int)Math.Ceiling((double)items.Count / chunkSize));

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(pageCount)
            .WithPageFactory(p => GeneratePage(p, items, entityViewer, chunkSize))
            .Build();

        await Service.SendPaginatorAsync(paginator, Context.Channel,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true);
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
        => await Service.TryFindEntityV2Async(
            Context,
            collection ?? await GetCollectionAsync(),
            EntityViewer,
            Id,
            id,
            timeout: TimeSpan.FromMinutes(2));

    private static IPage GeneratePage<TEntity>(
        IComponentPaginator p,
        IReadOnlyList<TEntity> items,
        Func<TEntity, EmbedBuilder> entityViewer,
        int chunkSize)
    {
        const uint accentColor = 0x9B59FF;
        const int maxItemChars = 650;

        var pageItems = items
            .Skip(p.CurrentPageIndex * chunkSize)
            .Take(chunkSize)
            .ToList();

        var container = new ContainerBuilder()
            .WithTextDisplay($"## Results ({items.Count})")
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        for (var i = 0; i < pageItems.Count; i++)
        {
            var embed = entityViewer(pageItems[i]).Build();
            container.WithSection(embed.ToComponentsV2Section(maxItemChars));

            if (i < pageItems.Count - 1)
                container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        container
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
            .WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary)
                .AddStopButton(p, "Close", ButtonStyle.Danger))
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Page {p.CurrentPageIndex + 1} of {p.PageCount}")
            .WithAccentColor(accentColor);

        var components = new ComponentBuilderV2().WithContainer(container).Build();
        return new PageBuilder().WithComponents(components).Build();
    }
}