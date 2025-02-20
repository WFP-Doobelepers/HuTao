using System.Collections.Generic;
using Discord;
using Discord.Commands;
using HuTao.Services.Interactive.Criteria;

namespace HuTao.Services.Interactive;

public class Prompt<T>(
    T key,
    string question,
    IEnumerable<EmbedFieldBuilder>? fields,
    bool isRequired,
    int? timeout,
    TypeReader? typeReader = null)
    : IPromptCriteria
    where T : notnull
{
    public bool IsRequired { get; } = isRequired;

    public IEnumerable<EmbedFieldBuilder>? Fields { get; } = fields;

    public int? Timeout { get; } = timeout;

    public string Question { get; } = question;

    public T Key { get; } = key;

    public ICollection<ICriterion<IMessage>>? Criteria { get; } = new List<ICriterion<IMessage>>();

    public TypeReader? TypeReader { get; set; } = typeReader;
}