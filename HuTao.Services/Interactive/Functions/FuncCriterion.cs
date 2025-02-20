using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace HuTao.Services.Interactive.Functions;

public class FuncCriterion(Func<SocketMessage, bool> func) : ICriterion<SocketMessage>
{
    public bool Judge(SocketCommandContext sourceContext, SocketMessage parameter)
        => func(parameter);

    public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        => Task.FromResult(func(parameter));
}