using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using MediatR;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Moderation;

namespace Zhongli.Bot.Behaviors
{
    public class ModerationLoggingBehavior : INotificationHandler<ReprimandNotification>
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

        public async Task Handle(ReprimandNotification notification,
            CancellationToken cancellationToken)
        {
            var (details, result) = notification;

            var reprimand = result.Primary;

            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.LoggingRules.Options;
            if (!options.HasFlag(LoggingOptions.Verbose)
                && reprimand.Source != ModerationSource.Command)
                return;

            var channelId = guild.LoggingRules.ModerationChannelId;
            if (channelId is null)
                return;

            var embed = await _moderationLogging.CreateEmbedAsync(details, result, cancellationToken);
            var channel = _client.GetGuild(guild.Id).GetTextChannel(channelId.Value);

            if (!options.HasFlag(LoggingOptions.Silent))
                await channel.SendMessageAsync(embed: embed.Build());

            if (options.HasFlag(LoggingOptions.NotifyUser))
            {
                var dm = await details.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync(
                    $"You have received an infraction from {details.Moderator.Guild}.",
                    embed: embed.Build());
            }
        }
    }
}