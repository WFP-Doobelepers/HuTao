using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Zhongli.Services.Interactive.Criteria;

namespace Zhongli.Services.Interactive;

public partial class PromptCollection<T> : IPromptCriteria where T : notnull
{
    public PromptCollection(
        InteractivePromptBase module,
        string? errorMessage = null, IServiceProvider? services = null)
    {
        ErrorMessage = errorMessage;
        Module       = module;
        Services     = services;
        Criteria = new ICriterion<IMessage>[]
        {
            new EnsureSourceChannelCriterion(),
            new EnsureSourceUserCriterion()
        };
    }

    public int Timeout { get; set; } = 30;

    public InteractivePromptBase Module { get; }

    public IServiceProvider? Services { get; }

    public List<Prompt<T>> Prompts { get; } = new();

    public SocketCommandContext Context => Module.Context;

    public string? ErrorMessage { get; set; }

    public ICollection<ICriterion<IMessage>> Criteria { get; }

    public TypeReader? TypeReader { get; set; }
}

public partial class PromptOrCollection<TOptions>
    where TOptions : notnull
{
    public PromptOrCollection(Prompt<TOptions> prompt, PromptCollection<TOptions> collection)
    {
        Prompt     = prompt;
        Collection = collection;
    }

    public Prompt<TOptions> Prompt { get; }

    public PromptCollection<TOptions> Collection { get; }
}