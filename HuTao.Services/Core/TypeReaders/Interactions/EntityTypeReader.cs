using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using HuTao.Data.Models.Discord;
using HuTao.Services.Interactive.Paginator;
using Microsoft.Extensions.DependencyInjection;
using InteractionContext = HuTao.Data.Models.Discord.InteractionContext;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public abstract class EntityTypeReader<T> : TypeReader<T?> where T : class
{
    private const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";

    public override async Task<TypeConverterResult> ReadAsync(
        IInteractionContext interaction, string option, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(option) || option.Equals("null", StringComparison.OrdinalIgnoreCase))
            return TypeConverterResult.FromSuccess(null);

        if (option.Length < 2)
            return TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, EmptyMatchMessage);

        var context = new InteractionContext(interaction);
        var collection = await GetCollectionAsync(context, services) ?? Enumerable.Empty<T>();

        var service = services.GetRequiredService<InteractiveService>();
        var result = await service.TryFindEntityAsync(context, collection, EntityViewer, Id, option);

        return result is not null
            ? TypeConverterResult.FromSuccess(result)
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, EmptyMatchMessage);
    }

    protected abstract EmbedBuilder EntityViewer(T entity);

    protected abstract string Id(T entity);

    protected abstract Task<ICollection<T>?> GetCollectionAsync(Context context, IServiceProvider services);
}