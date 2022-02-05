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
using Zhongli.Data.Config;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Core.TypeReaders;
using static Zhongli.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace Zhongli.Services.Core.Listeners;

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
        }
    }

    public async Task InitializeAsync()
    {
        _commands.CommandExecuted += CommandExecutedAsync;

        _commands.AddTypeReader<Color>(new HexColorTypeReader());

        _commands.AddTypeReader<IUser>(new TypeReaders.UserTypeReader<IUser>(CacheMode.AllowDownload, true));

        _commands.AddTypeReaders<IMessage>(
            new JumpUrlTypeReader(),
            new TypeReaders.MessageTypeReader<IMessage>());

        _commands.AddTypeReaders<IEmote>(
            new TryParseTypeReader<Emote>(Emote.TryParse),
            new TryParseTypeReader<Emoji>(Emoji.TryParse));

        _commands.AddTypeReader<RegexOptions>(
            new EnumFlagsTypeReader<RegexOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddTypeReader<GuildPermission>(
            new EnumFlagsTypeReader<GuildPermission>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddTypeReader<AuthorizationScope>(
            new EnumFlagsTypeReader<AuthorizationScope>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddTypeReader<ModerationLogOptions>(
            new EnumFlagsTypeReader<ModerationLogOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddTypeReader<LogReprimandType>(
            new EnumFlagsTypeReader<LogReprimandType>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddTypeReader<LogReprimandStatus>(
            new EnumFlagsTypeReader<LogReprimandStatus>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _commands.AddEnumerableTypeReader<LogType>(new EnumTryParseTypeReader<LogType>());

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task CommandExecutedAsync(
        Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (result.IsSuccess) return;

        if (result.Error is not CommandError.UnknownCommand)
        {
            _log.LogError("{Error}: {ErrorReason} in {Name}",
                result.Error, result.ErrorReason, command.Value?.Name);

            await _errorHandler.AssociateError(context.Message, $"{result.Error}: {result.ErrorReason}");
        }
    }
}