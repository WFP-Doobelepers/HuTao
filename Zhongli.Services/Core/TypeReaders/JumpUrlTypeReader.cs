using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.TypeReaders
{
    public class JumpUrlTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var client = services.GetRequiredService<DiscordSocketClient>();

            var message = await client.GetMessageFromUrlAsync(input);

            return message is null
                ? TypeReaderResult.FromError(CommandError.ObjectNotFound, "Message not found")
                : TypeReaderResult.FromSuccess(message);
        }
    }
}