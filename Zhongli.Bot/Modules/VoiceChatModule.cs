using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using MediatR;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Bot.Modules
{
    [Group("voice")]
    public class VoiceChatModule
    {
        public async Task ClaimAsync() { }

        public async Task CleanAsync() { }

        public async Task BanAsync() { }

        public async Task HideAsync() { }

        public async Task KickAsync() { }

        public async Task LimitAsync() { }

        public async Task LockAsync() { }

        public async Task OwnerAsync() { }

        public async Task RevealAsync() { }

        public async Task TransferAsync() { }

        public async Task UnbanAsync() { }

        public async Task UnlockAsync() { }
    }

    public class VoiceChatBehavior : INotificationHandler<UserVoiceStateNotification>
    {
        public async Task Handle(UserVoiceStateNotification notification, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}