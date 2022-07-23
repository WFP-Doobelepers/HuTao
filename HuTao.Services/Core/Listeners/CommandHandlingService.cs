using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Logging;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.TypeReaders.Commands;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace HuTao.Services.Core.Listeners;

public class CommandHandlingService : INotificationHandler<MessageReceivedNotification>
{
    private readonly CommandErrorHandler _errorHandler;
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _discord;
    private readonly HuTaoContext _db;
    private readonly ILogger<CommandHandlingService> _log;
    private readonly IServiceProvider _services;

    public CommandHandlingService(
        CommandErrorHandler errorHandler,
        CommandService commands,
        DiscordSocketClient discord,
        HuTaoContext db,
        ILogger<CommandHandlingService> log,
        IServiceProvider services)
    {
        _errorHandler = errorHandler;
        _commands     = commands;
        _discord      = discord;
        _db           = db;
        _log          = log;
        _services     = services;
    }

    [Priority(0)]
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var rawMessage = notification.Message;
        if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message)
            return;

        var argPos = 0;
        var prefix = HuTaoConfig.Configuration.Prefix;
        var hasPrefix = message.HasStringPrefix(prefix, ref argPos, StringComparison.OrdinalIgnoreCase);
        var hasMention = message.HasMentionPrefix(_discord.CurrentUser, ref argPos);

        if (hasPrefix || hasMention)
        {
            var context = new SocketCommandContext(_discord, message);
            if (context.User is IGuildUser user) await _db.Users.TrackUserAsync(user, cancellationToken);
            await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
        }
    }

    public async Task InitializeAsync()
    {
        _commands.CommandExecuted += CommandExecutedAsync;

        _commands.AddTypeReader<Color>(new HexColorTypeReader());
        _commands.AddTypeReader<ModerationCategory>(new CategoryTypeReader());
        _commands.AddTypeReader<SpamTypeReader>(new SpamTypeReader());

        _commands.AddInviteTypeReader<IInvite>();
        _commands.AddInviteTypeReader<IInviteMetadata>();
        _commands.AddInviteTypeReader<RestInviteMetadata>();

        _commands.AddUserTypeReader<IUser>();
        _commands.AddUserTypeReader<SocketUser>();
        _commands.AddUserTypeReader<RestUser>();

        _commands.AddUserTypeReader<IGuildUser>();
        _commands.AddUserTypeReader<SocketGuildUser>();
        _commands.AddUserTypeReader<RestGuildUser>();

        _commands.AddGuildTypeReader<IGuild>();
        _commands.AddGuildTypeReader<SocketGuild>();
        _commands.AddGuildTypeReader<RestGuild>();

        _commands.AddEnumerableTypeReader<LogType>(new EnumTryParseTypeReader<LogType>());

        _commands.AddTypeReaders<IUserMessage>(
            new JumpUrlTypeReader<IUserMessage>(),
            new TypeReaders.Commands.MessageTypeReader<IUserMessage>());

        _commands.AddTypeReaders<IMessage>(
            new JumpUrlTypeReader<IMessage>(),
            new TypeReaders.Commands.MessageTypeReader<IMessage>());

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

        _commands.AddTypeReader<UserTargetOptions>(
            new EnumFlagsTypeReader<UserTargetOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task CommandExecutedAsync(
        Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (result.IsSuccess) return;

        if (result.Error is not CommandError.UnknownCommand)
        {
            _log.LogError("{Error}: {ErrorReason} in {Name} by {User} in {Channel} {Guild}",
                result.Error, result.ErrorReason, command.Value?.Name,
                context.User, context.Channel, context.Guild);

            await _errorHandler.AssociateError(context.Message, $"{result.Error}: {result.ErrorReason}");
        }
    }
}