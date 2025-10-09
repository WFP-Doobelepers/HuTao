using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.TypeReaders.Interactions;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HuTao.Services.Core.Listeners;

public class InteractionHandlingService(
    DiscordSocketClient discord, HuTaoContext db,
    InteractionService commands, InteractiveService interactive,
    ILogger<InteractionHandlingService> log, IServiceProvider services)
    : INotificationHandler<InteractionCreatedNotification>,
      INotificationHandler<ReadyNotification>
{
    public async Task Handle(InteractionCreatedNotification notification, CancellationToken cancellationToken)
    {
        var interaction = notification.Interaction;

        if (interactive.IsManaged(interaction))
            return;

        var context = new SocketInteractionContext(discord, interaction);
        if (context.User is IGuildUser user) await db.Users.TrackUserAsync(user, cancellationToken);
        await commands.ExecuteCommandAsync(context, services);
    }

    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
#if DEBUG
        await commands.RegisterCommandsToGuildAsync(HuTaoConfig.Configuration.Guild);
#else
        var guildCommands = Array.Empty<ApplicationCommandProperties>();
        await discord.Rest.BulkOverwriteGuildCommands(guildCommands, HuTaoConfig.Configuration.Guild);
        await commands.RegisterCommandsGloballyAsync();
#endif
    }

    public async Task InitializeAsync()
    {
        commands.AutocompleteCommandExecuted += CommandExecutedAsync;
        commands.ComponentCommandExecuted    += CommandExecutedAsync;
        commands.ContextCommandExecuted      += CommandExecutedAsync;
        commands.ModalCommandExecuted        += CommandExecutedAsync;
        commands.SlashCommandExecuted        += CommandExecutedAsync;

        commands.AddUserTypeReader<IUser>();
        commands.AddUserTypeReader<SocketUser>();
        commands.AddUserTypeReader<RestUser>();

        commands.AddUserTypeReader<IGuildUser>();
        commands.AddUserTypeReader<SocketGuildUser>();
        commands.AddUserTypeReader<RestGuildUser>();

        commands.AddTypeReader<ModerationCategory>(new CategoryTypeReader());
        commands.AddTypeConverter<ModerationCategory>(new CategoryTypeConverter());

        commands.AddComponentTypeConverter<ModerationCategory>(new CategoryComponentTypeConverter());
        commands.AddComponentTypeConverter<TimeSpan>(new TimeSpanTypeConverter());

        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
    }

    private async Task CommandExecutedAsync(
        ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) return;

        if (result.Error is not InteractionCommandError.UnknownCommand)
        {
            log.LogError("{Error}: {ErrorReason} in {Name} by {User} in {Channel} {Guild}",
                result.Error, result.ErrorReason, command.Name,
                context.User, context.Channel, context.Guild);

            if (context.Interaction.HasResponded)
                await context.Interaction.FollowupAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
            else
                await context.Interaction.RespondAsync($"{result.Error}: {result.ErrorReason}", ephemeral: true);
        }
    }
}