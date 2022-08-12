using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class InviteTypeReader<T> : TypeReader where T : class, IInvite
{
    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services)
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var invite = await cache.ParseInviteAsync<T>(context.Client, input);

        return invite is null
            ? TypeReaderResult.FromError(CommandError.ObjectNotFound, "Invite not found.")
            : TypeReaderResult.FromSuccess(invite);
    }
}