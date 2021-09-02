using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
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
            await LogReprimandAsync(embed, reprimand, details.User, details.Moderator, cancellationToken);
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
            await LogReprimandAsync(embed, reprimand, details.User, details.Moderator, cancellationToken);

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
            embed.AddField("Total", await GetTotalAsync(reprimand, cancellationToken), true);

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
                var total = await GetTotalAsync(reprimand, cancellationToken);
                embed
                    .WithColor(reprimand.GetColor())
                    .AddField($"{reprimand.GetTitle()} [{total}]", $"{reprimand.GetMessage()}");
            }
        }

        private async Task LogReprimandAsync(
            EmbedBuilder embed, ReprimandResult result,
            IUser user, IGuildUser moderator,
            CancellationToken cancellationToken)
        {
            var reprimand = result.Primary;
            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.LoggingRules.Options;

            if (!options.HasFlag(LoggingOptions.Verbose)
                && reprimand.Status is ReprimandStatus.Added
                && reprimand.TriggerId is not null)
            {
                var trigger = await reprimand.GetTriggerAsync(_db, cancellationToken);
                if (trigger is ReprimandTrigger { Source: TriggerSource.Warning or TriggerSource.Notice })
                    return;
            }

            var channelId = guild.LoggingRules.ModerationChannelId;
            if (channelId is null)
                return;

            AddReprimandAuthor(moderator, false, embed);
            var channel = await moderator.Guild.GetTextChannelAsync(channelId.Value);
            _ = channel.SendMessageAsync(embed: embed.Build());

            if (options.HasFlag(LoggingOptions.NotifyUser)
                && reprimand is not Note
                && reprimand.IsIncluded(guild.LoggingRules.NotifyReprimands)
                && reprimand.Status
                    is ReprimandStatus.Added
                    or ReprimandStatus.Expired
                    or ReprimandStatus.Updated)
            {
                AddReprimandAuthor(moderator, options.HasFlag(LoggingOptions.Anonymous), embed);
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

        private async ValueTask<uint> GetTotalAsync(Reprimand reprimand, CancellationToken cancellationToken)
        {
            var user = await reprimand.GetUserAsync(_db, cancellationToken);

            return reprimand switch
            {
                Ban      => user.HistoryCount<Ban>(),
                Censored => user.HistoryCount<Censored>(),
                Kick     => user.HistoryCount<Kick>(),
                Mute     => user.HistoryCount<Mute>(),
                Note     => user.HistoryCount<Note>(),
                Notice   => user.HistoryCount<Notice>(),
                Warning  => user.WarningCount(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(reprimand), reprimand, "An unknown reprimand was given.")
            };
        }

        private static void AddReason(EmbedBuilder embed, ModerationAction? action)
        {
            if (!string.IsNullOrWhiteSpace(action?.Reason))
                embed.AddField("Reason", action.Reason);
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