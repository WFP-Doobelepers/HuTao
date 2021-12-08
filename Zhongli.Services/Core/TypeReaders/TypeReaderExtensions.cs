using System.Collections.Generic;
using Discord.Commands;

namespace Zhongli.Services.Core.TypeReaders;

public static class TypeReaderExtensions
{
    public static CommandService AddEnumerableTypeReader<TResult, TReader>(
        this CommandService commands) where TReader : TypeReader, new()
        => commands.AddEnumerableTypeReader<TResult>(new TReader());

    public static CommandService AddEnumerableTypeReader<TResult>(
        this CommandService commands, TypeReader typeReader)
    {
        var reader = new EnumerableTypeReader<TResult>(typeReader);

        commands.AddTypeReader<IEnumerable<TResult>>(reader);

        commands.AddTypeReader<List<TResult>>(reader);
        commands.AddTypeReader<IList<TResult>>(reader);
        commands.AddTypeReader<IReadOnlyList<TResult>>(reader);

        commands.AddTypeReader<TResult[]>(reader);
        commands.AddTypeReader<ICollection<TResult>>(reader);
        commands.AddTypeReader<IReadOnlyCollection<TResult>>(reader);

        return commands;
    }
}