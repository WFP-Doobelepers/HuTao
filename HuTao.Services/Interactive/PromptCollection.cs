using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using HuTao.Services.Interactive.Criteria;

namespace HuTao.Services.Interactive;

public partial class PromptCollection<T>(
    InteractivePromptBase module,
    string? errorMessage = null,
    IServiceProvider? services = null)
    : IPromptCriteria
    where T : notnull
{
    public int Timeout { get; set; } = 30;

    public InteractivePromptBase Module { get; } = module;

    public IServiceProvider? Services { get; } = services;

    public List<Prompt<T>> Prompts { get; } = [];

    public SocketCommandContext Context => Module.Context;

    public string? ErrorMessage { get; set; } = errorMessage;

    public ICollection<ICriterion<IMessage>> Criteria { get; } =
    [
        new EnsureSourceChannelCriterion(),
        new EnsureSourceUserCriterion()
    ];

    public TypeReader? TypeReader { get; set; }
}

public partial class PromptOrCollection<TOptions>(Prompt<TOptions> prompt, PromptCollection<TOptions> collection)
    where TOptions : notnull
{
    public Prompt<TOptions> Prompt { get; } = prompt;

    public PromptCollection<TOptions> Collection { get; } = collection;
}