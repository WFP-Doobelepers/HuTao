using System;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive.TypeReaders;

public class TypeReaderCriterion : ICriterion<SocketMessage>
{
    private readonly IServiceProvider? _services;
    private readonly TypeReader _reader;

    public TypeReaderCriterion(TypeReader reader, IServiceProvider? services = null)
    {
        _reader   = reader;
        _services = services;
    }

    public bool Judge(SocketCommandContext sourceContext, SocketMessage parameter)
        => _reader.ReadAsync(sourceContext, parameter.Content, _services).Result.IsSuccess;
}