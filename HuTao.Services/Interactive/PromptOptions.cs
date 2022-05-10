using System.Collections.Generic;
using Discord;
using HuTao.Services.Interactive.Criteria;

namespace HuTao.Services.Interactive;

internal class PromptOptions
{
    public bool IsRequired { get; init; }

    public Color? Color { get; init; }

    public CriteriaCriterion<IMessage>? Criterion { get; init; }

    public IEnumerable<EmbedFieldBuilder>? Fields { get; init; }

    public int SecondsTimeout { get; init; }
}