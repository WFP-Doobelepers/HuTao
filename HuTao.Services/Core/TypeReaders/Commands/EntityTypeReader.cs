using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using HuTao.Data.Models.Discord;
using HuTao.Services.Interactive.Paginator;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Core.TypeReaders.Commands;

public abstract class EntityTypeReader<T> : TypeReader where T : class
{
    private const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";

    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext command, string input, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(input) || input.Equals("null", StringComparison.OrdinalIgnoreCase))
            return TypeReaderResult.FromSuccess(null);

        if (input.Length < 2)
            return TypeReaderResult.FromError(CommandError.ObjectNotFound, EmptyMatchMessage);

        var context = new CommandContext(command);
        var collection = await GetCollectionAsync(context, services) ?? Enumerable.Empty<T>();

        var service = services.GetRequiredService<InteractiveService>();
        var result = await service.TryFindEntityAsync(context, collection, EntityViewer, Id, input);

        return result is not null
            ? TypeReaderResult.FromSuccess(result)
            : TypeReaderResult.FromError(CommandError.ObjectNotFound, EmptyMatchMessage);
    }

    protected abstract EmbedBuilder EntityViewer(T entity);

    protected abstract string Id(T entity);

    protected abstract Task<ICollection<T>?> GetCollectionAsync(Context context, IServiceProvider services);
}