using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners
{
    public class CommandHandlingService : INotificationHandler<MessageReceivedNotification>
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<CommandHandlingService> _log;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services, ILogger<CommandHandlingService> log,
            CommandService commands, DiscordSocketClient discord)
        {
            _commands = commands;
            _services = services;
            _discord  = discord;
            _log      = log;

            _commands.CommandExecuted += CommandExecutedAsync;
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            var rawMessage = notification.Message;
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message)
                return;

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            var hasPrefix = message.HasStringPrefix("z!", ref argPos, StringComparison.OrdinalIgnoreCase);
            var hasMention = message.HasMentionPrefix(_discord.CurrentUser, ref argPos);
            if (hasPrefix || hasMention)
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
                if (result is null)
                    _log.LogWarning("Command with context {0} ran by user {1} is null.", context, message.Author);
                else if (!result.IsSuccess)
                    await CommandFailedAsync(context, result);
            }
        }

        private static Task CommandFailedAsync(ICommandContext context, IResult result) =>
            context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");

        private static Task CommandExecutedAsync(
            Optional<CommandInfo> command, ICommandContext context, IResult result) => Task.CompletedTask;

        public Task InitializeAsync() => _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }
}