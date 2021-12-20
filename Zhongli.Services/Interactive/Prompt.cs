using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Zhongli.Services.Interactive.Criteria;

namespace Zhongli.Services.Interactive;

public class Prompt<T> : IPromptCriteria<SocketMessage> where T : notnull
{
    public Prompt(
        T key, string question, IEnumerable<EmbedFieldBuilder>? fields, bool isRequired, int? timeout,
        TypeReader? typeReader = null)
    {
        Key        = key;
        Question   = question;
        Fields     = fields;
        IsRequired = isRequired;
        Timeout    = timeout;
        TypeReader = typeReader;
        Criteria   = new List<ICriterion<SocketMessage>>();
    }

    public bool IsRequired { get; }

    public IEnumerable<EmbedFieldBuilder>? Fields { get; }

    public int? Timeout { get; }

    public string Question { get; }

    public T Key { get; }

    public ICollection<ICriterion<SocketMessage>>? Criteria { get; }

    public TypeReader? TypeReader { get; set; }
}