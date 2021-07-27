using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ModerationLoggingBehavior :
        INotificationHandler<ModifiedReprimandNotification>,
        INotificationHandler<ReprimandNotification>
    {
        private readonly DiscordSocketClient _client;
        private readonly ZhongliContext _db;
        private readonly ModerationLoggingService _moderationLogging;

        public ModerationLoggingBehavior(DiscordSocketClient client, ZhongliContext db,
            ModerationLoggingService moderationLogging)
        {
            _client = client;
            _db     = db;

            _moderationLogging = moderationLogging;
        }

        public async Task Handle(ModifiedReprimandNotification notification, CancellationToken cancellationToken)
        {
            var (details, reprimand) = notification;

            var embed = await _moderationLogging.UpdatedEmbedAsync(details, reprimand, cancellationToken);
            await HandleReprimandAsync(reprimand, details.User, embed, cancellationToken);
        }

        public async Task Handle(ReprimandNotification notification,
            CancellationToken cancellationToken)
        {
            var (details, reprimand) = notification;

            var embed = await _moderationLogging.CreateEmbedAsync(details, reprimand, cancellationToken);
            await HandleReprimandAsync(reprimand.Primary, details.User, embed, cancellationToken);
        }

        private async Task HandleReprimandAsync(ReprimandAction reprimand, IUser user, EmbedBuilder embed,
            CancellationToken cancellationToken)
        {
            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.LoggingRules.Options;
            if (!options.HasFlag(LoggingOptions.Verbose)
                && reprimand.Source != ModerationSource.Command)
                return;

            var channelId = guild.LoggingRules.ModerationChannelId;
            if (channelId is null)
                return;

            var channel = _client.GetGuild(guild.Id).GetTextChannel(channelId.Value);
            await channel.SendMessageAsync(embed: embed.Build());

            if (options.HasFlag(LoggingOptions.NotifyUser) && reprimand is not Note
                && reprimand.Status is ReprimandStatus.Added or ReprimandStatus.Expired)
            {
                var dm = await user.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync(embed: embed.Build());
            }
        }
    }
}