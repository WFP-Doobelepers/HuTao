using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.TypeReaders.Interactions;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HuTao.Services.Core.Listeners;

public class InteractionHandlingService :
    INotificationHandler<InteractionCreatedNotification>,
    INotificationHandler<ReadyNotification>
{
    private readonly DiscordSocketClient _discord;
    private readonly HuTaoContext _db;
    private readonly ILogger<InteractionHandlingService> _log;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;

    public InteractionHandlingService(
        DiscordSocketClient discord,
        HuTaoContext db,
        InteractionService commands,
        ILogger<InteractionHandlingService> log,
        IServiceProvider services)
    {
        _discord  = discord;
        _db       = db;
        _commands = commands;
        _log      = log;
        _services = services;
    }

    public async Task Handle(InteractionCreatedNotification notification, CancellationToken cancellationToken)
    {
        var interaction = notification.Interaction;

        var context = new SocketInteractionContext(_discord, interaction);
        if (context.User is IGuildUser user) await _db.Users.TrackUserAsync(user, cancellationToken);
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
        _commands.AutocompleteCommandExecuted += CommandExecutedAsync;
        _commands.ComponentCommandExecuted    += CommandExecutedAsync;
        _commands.ContextCommandExecuted      += CommandExecutedAsync;
        _commands.ModalCommandExecuted        += CommandExecutedAsync;
        _commands.SlashCommandExecuted        += CommandExecutedAsync;

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
            _log.LogError("{Error}: {ErrorReason} in {Name} by {User} in {Channel} {Guild}",
                result.Error, result.ErrorReason, command.Name,
                context.User, context.Channel, context.Guild);

            if (context.Interaction.HasResponded)
                await context.Interaction.FollowupAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
            else
                await context.Interaction.RespondAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
        }
    }
}