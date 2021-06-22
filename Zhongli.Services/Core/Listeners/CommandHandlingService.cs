using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Core.TypeReaders;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Core.Listeners
{
    public class CommandHandlingService : INotificationHandler<MessageReceivedNotification>
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly CommandErrorHandler _errorHandler;
        private readonly ILogger<CommandHandlingService> _log;
        private readonly IServiceProvider _services;

        public CommandHandlingService(
            IServiceProvider services, ILogger<CommandHandlingService> log,
            CommandService commands, CommandErrorHandler errorHandler,
            DiscordSocketClient discord)
        {
            _services = services;
            _log      = log;

            _commands     = commands;
            _errorHandler = errorHandler;

            _discord = discord;

            _commands.CommandExecuted += CommandExecutedAsync;
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            var rawMessage = notification.Message;
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message)
                return;

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            // Make sure the server entity exists
            await using var db = _services.GetRequiredService<ZhongliContext>();
            if (await db.Guilds.FindByIdAsync(context.Guild.Id, cancellationToken) is null)
                db.Add(new GuildEntity(context.Guild.Id));

            // Make sure the user entity exists
            if (await db.Users.FindAsync(new object[] { context.User.Id, context.Guild.Id }, cancellationToken) is null)
                db.Add(new GuildUserEntity((IGuildUser) context.User));

            await db.SaveChangesAsync(cancellationToken);

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

        private async Task CommandFailedAsync(ICommandContext context, IResult result)
        {
            var error = $"{result.Error}: {result.ErrorReason}";

            if (string.Equals(result.ErrorReason, "UnknownCommand", StringComparison.OrdinalIgnoreCase))
                Log.Error(error);
            else
                Log.Warning(error);

            if (result.Error == CommandError.Exception)
                await context.Channel.SendMessageAsync(
                    $"Error: {FormatUtilities.SanitizeEveryone(result.ErrorReason)}");
            else
                await _errorHandler.AssociateError(context.Message, error);
        }

        private static Task CommandExecutedAsync(
            Optional<CommandInfo> command, ICommandContext context, IResult result) => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            _commands.AddTypeReader<IMessage>(new TypeReaders.MessageTypeReader<IMessage>());
            _commands.AddTypeReader<IMessage>(new JumpUrlTypeReader());
    
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}