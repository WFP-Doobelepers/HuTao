using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Zhongli.Data.Config;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners;

public class InteractionHandlingService :
    INotificationHandler<InteractionCreatedNotification>,
    INotificationHandler<ReadyNotification>
{
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<InteractionHandlingService> _log;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;

    public InteractionHandlingService(
        InteractionService commands,
        DiscordSocketClient discord,
        ILogger<InteractionHandlingService> log,
        IServiceProvider services)
    {
        _commands = commands;
        _discord  = discord;
        _log      = log;
        _services = services;
    }

    public async Task Handle(InteractionCreatedNotification notification, CancellationToken cancellationToken)
    {
        var interaction = notification.Interaction;

        var context = new SocketInteractionContext(_discord, interaction);
        var result = await _commands.ExecuteCommandAsync(context, _services);

        if (!result.IsSuccess)
            await InteractionFailedAsync(context, result);
    }

    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
#if DEBUG
        await _commands.RegisterCommandsToGuildAsync(ZhongliConfig.Configuration.Guild);
#else
        await _commands.RegisterCommandsGloballyAsync();
#endif
    }

    public async Task InitializeAsync()
    {
        _commands.SlashCommandExecuted     += SlashCommandExecuted;
        _commands.ContextCommandExecuted   += ContextCommandExecuted;
        _commands.ComponentCommandExecuted += ComponentCommandExecuted;

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private static Task ComponentCommandExecuted(ComponentCommandInfo info, IInteractionContext context, IResult result)
        => Task.CompletedTask;

    private static Task ContextCommandExecuted(ContextCommandInfo info, IInteractionContext context, IResult result)
        => Task.CompletedTask;

    private async Task InteractionFailedAsync(IInteractionContext context, IResult result)
    {
        var error = $"{result.Error}: {result.ErrorReason}";

        if (result.Error is not InteractionCommandError.UnknownCommand)
        {
            _log.LogError("{Error}: {ErrorReason}", result.Error, result.ErrorReason);
            await context.Interaction.FollowupAsync(error, ephemeral: true);
        }
    }

    private static Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, IResult result)
        => Task.CompletedTask;
}