using Discord.Commands;
using Discord.WebSocket;

namespace HuTao.Services.Interactive.TryParse;

public class TryParseCriterion<T> : ICriterion<SocketMessage>
{
    private readonly TryParseDelegate<T> _tryParse;

    public TryParseCriterion(TryParseDelegate<T> tryParse) { _tryParse = tryParse; }

    public bool Judge(SocketCommandContext sourceContext, SocketMessage parameter) =>
        _tryParse(parameter.Content, out _);
}