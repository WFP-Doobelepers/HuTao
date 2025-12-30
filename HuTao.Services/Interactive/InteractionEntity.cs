using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Data;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;

namespace HuTao.Services.Interactive;

public abstract class InteractionEntity<T> : InteractionModuleBase<SocketInteractionContext> where T : class
{
    protected const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";

    public CommandErrorHandler Error { get; init; } = null!;

    public HuTaoContext Db { get; init; } = null!;

    public InteractiveService Interactive { get; init; } = null!;

    protected virtual int ChunkSize => 6;

    protected abstract EmbedBuilder EntityViewer(T entity);

    protected abstract string Id(T entity);

    protected async Task AddEntityAsync(T entity)
    {
        var collection = await GetCollectionAsync();
        collection.Add(entity);

        await Db.SaveChangesAsync();
    }

    protected async Task PagedViewAsync(IEnumerable<T> collection, bool ephemeral = false)
        => await PagedViewAsync(collection, EntityViewer, ephemeral);

    protected async Task PagedViewAsync<TEntity>(
        IEnumerable<TEntity> collection,
        Func<TEntity, EmbedBuilder> entityViewer, bool ephemeral = false)
    {
        var items = collection.ToList();
        if (items.Count == 0)
        {
            await FollowupAsync("No results found.", ephemeral: ephemeral);
            return;
        }

        var chunkSize = ChunkSize;
        var pageCount = Math.Max(1, (int)Math.Ceiling((double)items.Count / chunkSize));

        var paginator = InteractiveExtensions.CreateDefaultComponentPaginator()
            .WithUsers(Context.User)
            .WithPageCount(pageCount)
            .WithPageFactory(p => GeneratePage(p, items, entityViewer, chunkSize))
            .Build();

        await Interactive.SendPaginatorAsync(
            paginator,
            Context.Interaction,
            ephemeral: ephemeral,
            timeout: TimeSpan.FromMinutes(10),
            resetTimeoutOnInput: true,
            responseType: Context.Interaction.HasResponded
                ? InteractionResponseType.DeferredChannelMessageWithSource
                : InteractionResponseType.ChannelMessageWithSource);
    }

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

    protected virtual async Task RemoveEntityAsync(string id, bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);

        if (entity is null)
        {
            await FollowupAsync(EmptyMatchMessage, ephemeral: true);
            return;
        }

        await RemoveEntityAsync(entity, ephemeral);
    }

    protected virtual async Task RemoveEntityAsync(T entity, bool ephemeral)
    {
        Db.Remove(entity);
        await Db.SaveChangesAsync();
    }

    protected virtual async Task ViewEntityAsync()
    {
        await DeferAsync();
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    protected abstract Task<ICollection<T>> GetCollectionAsync();

    protected async Task<T?> TryFindEntityAsync(string id, IEnumerable<T>? collection = null)
    {
        var ctx = (HuTao.Data.Models.Discord.Context)Context;
        return await Interactive.TryFindEntityV2Async(
            ctx,
            collection ?? await GetCollectionAsync(),
            EntityViewer,
            Id,
            id,
            ephemeral: true,
            timeout: TimeSpan.FromMinutes(2));
    }
}