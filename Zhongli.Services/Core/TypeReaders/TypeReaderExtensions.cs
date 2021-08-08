using System.Collections.Generic;
using Discord.Commands;

namespace Zhongli.Services.Core.TypeReaders
{
    public static class TypeReaderExtensions
    {
        public static CommandService AddEnumerableTypeReader<TResult, TReader>(this CommandService commands)
            where TReader : TypeReader, new()
            where TResult : class
        {
            commands.AddTypeReader<IEnumerable<TResult>>(new EnumerableTypeReader<TReader, TResult>());
            return commands;
        }
    }
}