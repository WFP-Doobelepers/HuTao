using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Zhongli.Services.Core.TypeReaders
{
    public class EmoteTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services) =>
            Emote.TryParse(input, out var emote)
                ? Task.FromResult(TypeReaderResult.FromSuccess(emote))
                : Task.FromResult(TypeReaderResult.FromSuccess(new Emoji(input)));
    }
}