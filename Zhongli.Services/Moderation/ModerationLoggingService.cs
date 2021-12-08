using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public class ModerationLoggingService
    {
        private readonly ZhongliContext _db;

        public ModerationLoggingService(ZhongliContext db) { _db = db; }

        public async Task PublishReprimandAsync(Reprimand reprimand, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = await UpdatedEmbedAsync(reprimand, details, cancellationToken);
            await LogReprimandAsync(embed, reprimand, details, cancellationToken);
        }

        public async Task<EmbedBuilder> CreateEmbedAsync(ReprimandResult result, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User);

            await AddPrimaryAsync(embed, result.Primary, cancellationToken);
            foreach (var secondary in result.Secondary)
            {
                await AddSecondaryAsync(embed, secondary, cancellationToken);
            }

            return embed;
        }

        public async Task<EmbedBuilder> UpdatedEmbedAsync(Reprimand reprimand, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User)
                .WithTitle($"{reprimand.Status.Humanize()} {reprimand.GetTitle()}")
                .WithColor(Color.Purple);

            AddReason(embed, reprimand.ModifiedAction);
            await AddReprimandDetailsAsync(embed, reprimand, cancellationToken);

            return embed;
        }

        public async Task<ReprimandResult> PublishReprimandAsync(ReprimandResult reprimand, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = await CreateEmbedAsync(reprimand, details, cancellationToken);
            await LogReprimandAsync(embed, reprimand, details, cancellationToken);

            return reprimand;
        }

        private static EmbedBuilder CreateReprimandEmbed(IUser user) => new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithCurrentTimestamp();

        private async Task AddPrimaryAsync(EmbedBuilder embed, Reprimand? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                embed
                    .WithTitle(reprimand.GetTitle())
                    .WithColor(reprimand.GetColor())
                    .WithDescription(reprimand.GetMessage());

                AddReason(embed, reprimand.Action);
                await AddReprimandDetailsAsync(embed, reprimand, cancellationToken);
            }
        }

        private async Task AddReprimandDetailsAsync(EmbedBuilder embed, Reprimand reprimand,
            CancellationToken cancellationToken)
        {
            embed
                .AddField("Active", await GetTotalAsync(reprimand, false, cancellationToken), true)
                .AddField("Total", await GetTotalAsync(reprimand, true, cancellationToken), true);

            var trigger = await reprimand.GetTriggerAsync(_db, cancellationToken);
            if (trigger is null) return;

            embed
                .AddField("Trigger", trigger.GetTriggerDetails(), true)
                .AddField("Trigger ID", trigger.Id, true);
        }

        private async Task AddSecondaryAsync(EmbedBuilder embed, Reprimand? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                var active = await GetTotalAsync(reprimand, false, cancellationToken);
                var total = await GetTotalAsync(reprimand, true, cancellationToken);

                embed
                    .WithColor(reprimand.GetColor())
                    .AddField($"{reprimand.GetTitle()} [{active}/{total}]", $"{reprimand.GetMessage()}");
            }
        }

        private async Task LogReprimandAsync(EmbedBuilder embed,
            ReprimandResult result, ReprimandDetails details,
            CancellationToken cancellationToken)
        {
            var (user, moderator, _, _) = details;
            var reprimand = result.Primary;
            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.ModerationRules.Options;

            if (!options.HasFlag(ReprimandOptions.Verbose)
                && reprimand.Status is ReprimandStatus.Added
                && reprimand.TriggerId is not null)
            {
                var trigger = await reprimand.GetTriggerAsync(_db, cancellationToken);
                if (trigger is ReprimandTrigger { Source: TriggerSource.Warning or TriggerSource.Notice })
                    return;
            }

            var type = reprimand.GetReprimandType();
            var channel = await GetLoggingChannelAsync(type, moderator.Guild, cancellationToken);
            if (channel is null) return;

            AddReprimandAuthor(moderator, false, embed);
            _ = channel.SendMessageAsync(embed: embed.Build());

            if (options.HasFlag(ReprimandOptions.NotifyUser)
                && reprimand is not Note
                && reprimand.IsIncluded(guild.LoggingRules.NotifyReprimands)
                && reprimand.Status
                    is ReprimandStatus.Added
                    or ReprimandStatus.Expired
                    or ReprimandStatus.Updated)
            {
                AddReprimandAuthor(moderator, options.HasFlag(ReprimandOptions.Anonymous), embed);
                if (reprimand.IsIncluded(guild.LoggingRules.ShowAppealOnReprimands))
                {
                    var appealMessage = guild.LoggingRules.ReprimandAppealMessage;
                    if (!string.IsNullOrWhiteSpace(appealMessage))
                        embed.AddField("Appeal", appealMessage);
                }

                var dm = await user.GetOrCreateDMChannelAsync();
                _ = dm?.SendMessageAsync(embed: embed.Build());
            }
        }

        private async Task<IMessageChannel?> GetLoggingChannelAsync(ReprimandType type, IGuild guild,
            CancellationToken cancellationToken)
        {
            var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);

            var channel = guildEntity.ModerationRules.LoggingChannels
                .FirstOrDefault(r => r.Type == type);

            if (channel is null) return null;
            return await guild.GetTextChannelAsync(channel.ChannelId);
        }

        private async ValueTask<uint> GetTotalAsync(Reprimand reprimand,
            bool countHidden = true,
            CancellationToken cancellationToken = default)
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);

            return reprimand switch
            {
                Ban      => user.HistoryCount<Ban>(countHidden),
                Censored => user.HistoryCount<Censored>(countHidden),
                Kick     => user.HistoryCount<Kick>(countHidden),
                Mute     => user.HistoryCount<Mute>(countHidden),
                Note     => user.HistoryCount<Note>(countHidden),
                Notice   => user.HistoryCount<Notice>(countHidden),
                Warning  => user.WarningCount(countHidden),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(reprimand), reprimand, "An unknown reprimand was given.")
            };
        }

        private static void AddReason(EmbedBuilder embed, ModerationAction? action)
        {
            if (string.IsNullOrWhiteSpace(action?.Reason)) return;

            var reason = action.Reason.Length > 1024
                ? $"{action.Reason[..1021]}..."
                : action.Reason;

            embed.AddField("Reason", reason);
        }

        private static void AddReprimandAuthor(IGuildUser moderator, bool isAnonymous, EmbedBuilder embed)
        {
            if (isAnonymous)
                embed.WithGuildAsAuthor(moderator.Guild, AuthorOptions.UseFooter | AuthorOptions.Requested);
            else
                embed.WithUserAsAuthor(moderator, AuthorOptions.UseFooter | AuthorOptions.Requested);
        }
    }
}