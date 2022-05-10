using System;
using Discord.WebSocket;

namespace HuTao.Services.Interactive.Functions;

public static class FuncCriterionExtensions
{
    public static FuncCriterion AsCriterion(this Func<SocketMessage, bool> func) => new(func);
}