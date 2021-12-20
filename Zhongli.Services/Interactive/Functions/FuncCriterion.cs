using System;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive.Functions;

public class FuncCriterion : ICriterion<SocketMessage>
{
    private readonly Func<SocketMessage, bool> _func;

    public FuncCriterion(Func<SocketMessage, bool> func) { _func = func; }

    public bool Judge(SocketCommandContext sourceContext, SocketMessage parameter)
        => _func(parameter);
}