using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using Zhongli.Data.Config;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Core.TypeReaders;
using Zhongli.Services.Interactive.TypeReaders;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.Listeners
{
    public class CommandHandlingService : INotificationHandler<MessageReceivedNotification>
    {
        private readonly CommandErrorHandler _errorHandler;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<CommandHandlingService> _log;
        private readonly IServiceProvider _services;

        public CommandHandlingService(
            CommandErrorHandler errorHandler,
            CommandService commands,
            DiscordSocketClient discord,
            ILogger<CommandHandlingService> log,
            IServiceProvider services)
        {
            _errorHandler = errorHandler;
            _commands     = commands;
            _discord      = discord;
            _log          = log;
            _services     = services;

            _commands.CommandExecuted += CommandExecutedAsync;
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            var rawMessage = notification.Message;
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message)
                return;

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            var hasPrefix = message.HasStringPrefix(ZhongliConfig.Configuration.Prefix, ref argPos,
                StringComparison.OrdinalIgnoreCase);
            var hasMention = message.HasMentionPrefix(_discord.CurrentUser, ref argPos);
            if (hasPrefix || hasMention)
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
                if (result is null)
                {
                    _log.LogWarning("Command on guild {Guild} ran by user {Author} is null", context.Guild,
                        message.Author);
                }
                else if (!result.IsSuccess)
                    await CommandFailedAsync(context, result);
            }
        }

        public async Task InitializeAsync()
        {
            _commands.AddTypeReader<IMessage>(new TypeReaders.MessageTypeReader<IMessage>());
            _commands.AddTypeReader<IUserMessage>(new TypeReaders.MessageTypeReader<IMessage>());

            _commands.AddTypeReader<IMessage>(new JumpUrlTypeReader());

            _commands.AddTypeReader<RegexOptions>(
                new EnumFlagsTypeReader<RegexOptions>(
                    splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

            _commands.AddTypeReader<Guid>(new TryParseTypeReader<Guid>(Guid.TryParse));

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private static Task CommandExecutedAsync(
            Optional<CommandInfo> command, ICommandContext context, IResult result) => Task.CompletedTask;

        private async Task CommandFailedAsync(ICommandContext context, IResult result)
        {
            var error = $"{result.Error}: {result.ErrorReason}";

            if (string.Equals(result.ErrorReason, "UnknownCommand", StringComparison.OrdinalIgnoreCase))
                Log.Warning("{Error}: {ErrorReason}", result.Error, result.ErrorReason);
            else
                Log.Error("{Error}: {ErrorReason}", result.Error, result.ErrorReason);

            if (result.Error == CommandError.Exception)
            {
                await context.Channel.SendMessageAsync(
                    $"Error: {FormatUtilities.SanitizeEveryone(result.ErrorReason)}");
            }
            else
                await _errorHandler.AssociateError(context.Message, error);
        }
    }
}