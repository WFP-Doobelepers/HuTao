using System;
using Discord;
using Discord.Commands;

namespace Zhongli.Services.Interactive.TypeReaders;

public class TypeReaderCriterion : ICriterion<IMessage>
{
    private readonly IServiceProvider? _services;
    private readonly TypeReader _reader;

    public TypeReaderCriterion(TypeReader reader, IServiceProvider? services = null)
    {
        _reader   = reader;
        _services = services;
    }

    public bool Judge(SocketCommandContext sourceContext, IMessage parameter)
        => _reader.ReadAsync(sourceContext, parameter.Content, _services).Result.IsSuccess;
}