using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Censors;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public class ModerationLoggingService
    {
        private readonly ZhongliContext _db;

        public ModerationLoggingService(ZhongliContext db) { _db = db; }

        public static string GetTitle(Reprimand action)
        {
            var title = action switch
            {
                Ban      => nameof(Ban),
                Censored => nameof(Censored),
                Kick     => nameof(Kick),
                Mute     => nameof(Mute),
                Note     => nameof(Note),
                Notice   => nameof(Notice),
                Warning  => nameof(Warning),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            return $"{title.Humanize()}: {action.Id}";
        }

        public static StringBuilder GetReprimandDetails(Reprimand r)
        {
            var content = new StringBuilder()
                .AppendLine($"▌{GetMessage(r)}")
                .AppendLine($"▌Reason: {r.GetReason()}")
                .AppendLine($"▌Moderator: {r.GetModerator()}")
                .AppendLine($"▌Date: {r.GetDate()}")
                .AppendLine("▌")
                .AppendLine($"▌Status: {Format.Bold(r.Status.Humanize())}");

            if (r.Status is not ReprimandStatus.Added && r.ModifiedAction is not null)
            {
                content
                    .AppendLine($"▌▌{r.Status.Humanize()} by {r.ModifiedAction.GetModerator()}")
                    .AppendLine($"▌▌{r.ModifiedAction.GetDate()}")
                    .AppendLine($"▌▌{r.ModifiedAction.GetReason()}");
            }

            if (r.Trigger is not null)
            {
                content
                    .AppendLine("▌")
                    .AppendLine($"▌Trigger: {r.TriggerId}")
                    .AppendLine($"▌▌{GetTriggerDetails(r.Trigger)}");
            }

            return content;
        }

        public async Task PublishReprimandAsync(Reprimand reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
        {
            var embed = await UpdatedEmbedAsync(reprimand, details, cancellationToken);
            await LogReprimandAsync(embed, reprimand, details.User, details.Moderator, cancellationToken);
        }

        public async Task<EmbedBuilder> CreateEmbedAsync(ReprimandResult result, ReprimandDetails details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User);

            await AddPrimaryAsync(embed, details, result.Primary, cancellationToken);
            foreach (var secondary in result.Secondary)
            {
                await AddSecondaryAsync(embed, details, secondary, cancellationToken);
            }

            return embed;
        }

        public async Task<EmbedBuilder> UpdatedEmbedAsync(Reprimand reprimand, ModifiedReprimand details,
            CancellationToken cancellationToken = default)
        {
            var embed = CreateReprimandEmbed(details.User)
                .WithTitle($"{reprimand.Status.Humanize()} {GetTitle(reprimand)}")
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

        private static bool IsIncluded(Reprimand reprimand, ReprimandNoticeType type)
        {
            if (type == ReprimandNoticeType.All)
                return true;

            return reprimand switch
            {
                Ban      => type.HasFlag(ReprimandNoticeType.Ban),
                Censored => type.HasFlag(ReprimandNoticeType.Censor),
                Kick     => type.HasFlag(ReprimandNoticeType.Kick),
                Mute     => type.HasFlag(ReprimandNoticeType.Mute),
                Notice   => type.HasFlag(ReprimandNoticeType.Notice),
                Warning  => type.HasFlag(ReprimandNoticeType.Warning),
                _        => false
            };
        }

        private static Color GetColor(Reprimand reprimand)
        {
            return reprimand switch
            {
                Ban      => Color.Red,
                Censored => Color.Blue,
                Kick     => Color.Red,
                Mute     => Color.Orange,
                Note     => Color.Blue,
                Notice   => Color.Gold,
                Warning  => Color.Gold,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(reprimand), reprimand, "An unknown reprimand was given.")
            };
        }

        private static EmbedBuilder CreateReprimandEmbed(IUser user) => new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithCurrentTimestamp();

        private static string GetMessage(Reprimand action)
        {
            var mention = $"<@{action.UserId}>";
            return action switch
            {
                Ban        => $"{mention} was banned.",
                Censored c => $"{mention} was censored. Message: {c.CensoredMessage()}",
                Kick       => $"{mention} was kicked.",
                Mute m     => $"{mention} was muted for {GetLength(m)}.",
                Note       => $"{mention} was given a note.",
                Notice     => $"{mention} was given a notice.",
                Warning w  => $"{mention} was warned {w.Count} times.",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            static string GetLength(ILength mute)
                => mute.Length?.Humanize(5,
                    minUnit: TimeUnit.Second,
                    maxUnit: TimeUnit.Year) ?? "indefinitely";
        }

        private static string GetTriggerDetails(Trigger trigger)
        {
            return trigger switch
            {
                Censor c           => $"{c.Mode} {c.Amount}: {Format.Code(c.Pattern)}",
                ReprimandTrigger r => $"{r.Mode} {r.Amount}: {r.Source.Humanize().Pluralize()}",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(trigger), trigger, "An unknown trigger was given.")
            };
        }

        private async Task AddPrimaryAsync(EmbedBuilder embed, ReprimandDetails details, Reprimand? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                embed
                    .WithTitle(GetTitle(reprimand))
                    .WithColor(GetColor(reprimand))
                    .WithDescription(GetMessage(reprimand));

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
                .AddField("Trigger", GetTriggerDetails(trigger), true)
                .AddField("Trigger ID", trigger.Id, true);
        }

        private async Task AddSecondaryAsync(EmbedBuilder embed, ReprimandDetails details, Reprimand? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                var total = await GetTotalAsync(reprimand, cancellationToken);
                embed
                    .WithColor(GetColor(reprimand))
                    .AddField($"{GetTitle(reprimand)} [{total}]", $"{GetMessage(reprimand)}");
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
                && IsIncluded(reprimand, guild.LoggingRules.NotifyReprimands)
                && reprimand.Status
                    is ReprimandStatus.Added
                    or ReprimandStatus.Expired
                    or ReprimandStatus.Updated)
            {
                AddReprimandAuthor(moderator, options.HasFlag(LoggingOptions.Anonymous), embed);
                if (IsIncluded(reprimand, guild.LoggingRules.ShowAppealOnReprimands))
                {
                    var appealMessage = guild.LoggingRules.ReprimandAppealMessage;
                    if (!string.IsNullOrWhiteSpace(appealMessage))
                        embed.AddField("Appeal", appealMessage);
                }

                var dm = await user.GetOrCreateDMChannelAsync();
                _ = dm?.SendMessageAsync(embed: embed.Build());
            }
        }

        private async Task<int> GetReprimandsOfTrigger(ITriggerAction action, Reprimand reprimand)
        {
            var user = await reprimand.GetUserAsync(_db);
            return user
                .Reprimands<Reprimand>()
                .Count(r => r.TriggerId == action.ReprimandId);
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