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

public class CommandHandlingService(
    CommandErrorHandler errorHandler,
    CommandService commands,
    DiscordSocketClient discord,
    HuTaoContext db,
    ILogger<CommandHandlingService> log,
    IServiceProvider services)
    : INotificationHandler<MessageReceivedNotification>
{
    [Priority(0)]
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var rawMessage = notification.Message;
        if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message)
            return;

        var argPos = 0;
        var prefix = HuTaoConfig.Configuration.Prefix;
        var hasPrefix = message.HasStringPrefix(prefix, ref argPos, StringComparison.OrdinalIgnoreCase);
        var hasMention = message.HasMentionPrefix(discord.CurrentUser, ref argPos);

        if (hasPrefix || hasMention)
        {
            var context = new SocketCommandContext(discord, message);
            var invoked = message.Content[argPos..].TrimStart();
            var invokedParts = invoked.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var invokedName = invokedParts.Length > 0 ? invokedParts[0] : string.Empty;
            log.LogDebug(
                "Prefix command received (Command={CommandName}, UserId={UserId}, ChannelId={ChannelId}, GuildId={GuildId}, MessageId={MessageId})",
                invokedName,
                context.User.Id,
                context.Channel.Id,
                context.Guild?.Id,
                message.Id);
            if (context.User is IGuildUser user) await db.Users.TrackUserAsync(user, cancellationToken);
            await commands.ExecuteAsync(context, argPos, services, MultiMatchHandling.Best);
        }
    }

    public async Task InitializeAsync()
    {
        commands.CommandExecuted += CommandExecutedAsync;

        commands.AddTypeReader<Color>(new HexColorTypeReader());
        commands.AddTypeReader<ModerationCategory>(new CategoryTypeReader());
        commands.AddTypeReader<SpamTypeReader>(new SpamTypeReader());

        commands.AddInviteTypeReader<IInvite>();
        commands.AddInviteTypeReader<IInviteMetadata>();
        commands.AddInviteTypeReader<RestInviteMetadata>();

        commands.AddUserTypeReader<IUser>();
        commands.AddUserTypeReader<SocketUser>();
        commands.AddUserTypeReader<RestUser>();

        commands.AddUserTypeReader<IGuildUser>();
        commands.AddUserTypeReader<SocketGuildUser>();
        commands.AddUserTypeReader<RestGuildUser>();

        commands.AddGuildTypeReader<IGuild>();
        commands.AddGuildTypeReader<SocketGuild>();
        commands.AddGuildTypeReader<RestGuild>();

        commands.AddEnumerableTypeReader<LogType>(new EnumTryParseTypeReader<LogType>());

        commands.AddTypeReaders<IUserMessage>(
            new JumpUrlTypeReader<IUserMessage>(),
            new TypeReaders.Commands.MessageTypeReader<IUserMessage>());

        commands.AddTypeReaders<IMessage>(
            new JumpUrlTypeReader<IMessage>(),
            new TypeReaders.Commands.MessageTypeReader<IMessage>());

        commands.AddTypeReaders<IEmote>(
            new TryParseTypeReader<Emote>(Emote.TryParse),
            new TryParseTypeReader<Emoji>(Emoji.TryParse));

        commands.AddTypeReader<RegexOptions>(
            new EnumFlagsTypeReader<RegexOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<GuildPermission>(
            new EnumFlagsTypeReader<GuildPermission>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<AuthorizationScope>(
            new EnumFlagsTypeReader<AuthorizationScope>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<ModerationLogOptions>(
            new EnumFlagsTypeReader<ModerationLogOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<LogReprimandType>(
            new EnumFlagsTypeReader<LogReprimandType>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<LogReprimandStatus>(
            new EnumFlagsTypeReader<LogReprimandStatus>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        commands.AddTypeReader<UserTargetOptions>(
            new EnumFlagsTypeReader<UserTargetOptions>(
                splitOptions: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
    }

    private async Task CommandExecutedAsync(
        Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (result.IsSuccess) return;

        if (result.Error is not CommandError.UnknownCommand)
        {
            log.LogError(
                "Command execution failed (Error={Error}, Reason={ErrorReason}, Command={CommandName}, UserId={UserId}, ChannelId={ChannelId}, GuildId={GuildId})",
                result.Error,
                result.ErrorReason,
                command.Value?.Name,
                context.User.Id,
                context.Channel.Id,
                context.Guild?.Id);

            await errorHandler.AssociateError(context.Message, $"{result.Error}: {result.ErrorReason}");
        }
    }
}