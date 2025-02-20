using Discord.WebSocket;
using MediatR;

namespace HuTao.Services.Core.Messages;

public class UserVoiceStateNotification(SocketUser user, SocketVoiceState old, SocketVoiceState @new)
    : INotification
{
    public SocketUser User { get; } = user;

    public SocketVoiceState New { get; } = @new;

    public SocketVoiceState Old { get; } = old;
}