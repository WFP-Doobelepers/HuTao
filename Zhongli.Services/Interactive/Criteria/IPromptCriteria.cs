using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive.Criteria;

public interface IPromptCriteria<T>
{
    ICollection<ICriterion<SocketMessage>>? Criteria { get; }

    TypeReader? TypeReader { get; }
}