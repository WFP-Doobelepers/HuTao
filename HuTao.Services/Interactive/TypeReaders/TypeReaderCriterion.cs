using System;
using Discord;
using Discord.Commands;

namespace HuTao.Services.Interactive.TypeReaders;

public class TypeReaderCriterion(TypeReader reader, IServiceProvider? services = null) : ICriterion<IMessage>
{
    public bool Judge(SocketCommandContext sourceContext, IMessage parameter)
        => reader.ReadAsync(sourceContext, parameter.Content, services).Result.IsSuccess;
}