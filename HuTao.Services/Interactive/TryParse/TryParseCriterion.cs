using Discord.Commands;
using Discord.WebSocket;

namespace HuTao.Services.Interactive.TryParse;

public class TryParseCriterion<T>(TryParseDelegate<T> tryParse) : ICriterion<SocketMessage>
{
    public bool Judge(SocketCommandContext sourceContext, SocketMessage parameter) =>
        tryParse(parameter.Content, out _);
}