using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class GuildTypeReader<T> : TypeReader where T : class, IGuild
{
    private readonly CacheMode _cacheMode;

    public GuildTypeReader(CacheMode cacheMode) { _cacheMode = cacheMode; }

    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services)
    {
        if (!ulong.TryParse(input, out var id))
            return TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid guild ID.");

        return await context.Client.GetGuildAsync(id, _cacheMode) is T guild
            ? TypeReaderResult.FromSuccess(guild)
            : TypeReaderResult.FromError(CommandError.ObjectNotFound, "Guild not found.");
    }
}