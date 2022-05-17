using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using HuTao.Data.Config;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.TypeReaders.Interactions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HuTao.Services.Core.Listeners;

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
        await _commands.ExecuteCommandAsync(context, _services);
    }

    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
#if DEBUG
        await _commands.RegisterCommandsToGuildAsync(HuTaoConfig.Configuration.Guild);
#else
        var guildCommands = Array.Empty<ApplicationCommandProperties>();
        await _discord.Rest.BulkOverwriteGuildCommands(guildCommands, HuTaoConfig.Configuration.Guild);
        await _commands.RegisterCommandsGloballyAsync();
#endif
    }

    public async Task InitializeAsync()
    {
        _commands.SlashCommandExecuted     += CommandExecutedAsync;
        _commands.ContextCommandExecuted   += CommandExecutedAsync;
        _commands.ComponentCommandExecuted += CommandExecutedAsync;

        _commands.AddUserTypeReader<IUser>();
        _commands.AddUserTypeReader<SocketUser>();
        _commands.AddUserTypeReader<RestUser>();

        _commands.AddUserTypeReader<IGuildUser>();
        _commands.AddUserTypeReader<SocketGuildUser>();
        _commands.AddUserTypeReader<RestGuildUser>();

        _commands.AddTypeReader<ModerationCategory>(new CategoryTypeReader());
        _commands.AddTypeConverter<ModerationCategory>(new CategoryTypeConverter());

        _commands.AddComponentTypeConverter<ModerationCategory>(new CategoryComponentTypeConverter());
        _commands.AddComponentTypeConverter<TimeSpan>(new TimeSpanTypeConverter());

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task CommandExecutedAsync(
        ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) return;

        if (result.Error is not InteractionCommandError.UnknownCommand)
        {
            _log.LogError("{Error}: {ErrorReason} in {Name}", result.Error, result.ErrorReason, command.Name);
            if (context.Interaction.HasResponded)
                await context.Interaction.FollowupAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
            else
                await context.Interaction.RespondAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
        }
    }
}