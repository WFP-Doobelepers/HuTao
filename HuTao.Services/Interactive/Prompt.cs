using System.Collections.Generic;
using Discord;
using Discord.Commands;
using HuTao.Services.Interactive.Criteria;

namespace HuTao.Services.Interactive;

public class Prompt<T> : IPromptCriteria where T : notnull
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
        Criteria   = new List<ICriterion<IMessage>>();
    }

    public bool IsRequired { get; }

    public IEnumerable<EmbedFieldBuilder>? Fields { get; }

    public int? Timeout { get; }

    public string Question { get; }

    public T Key { get; }

    public ICollection<ICriterion<IMessage>>? Criteria { get; }

    public TypeReader? TypeReader { get; set; }
}