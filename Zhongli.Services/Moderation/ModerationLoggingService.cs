using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using Zhongli.Data;
using Zhongli.Data.Models.Logging;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public class ModerationLoggingService
    {
        private readonly ZhongliContext _db;

        public ModerationLoggingService(ZhongliContext db) { _db = db; }

        public async Task<EmbedBuilder> UpdatedEmbedAsync(ModifiedReprimand details, ReprimandAction reprimand,
            CancellationToken cancellationToken = default)
        {
            var modifyName = reprimand.Status.Humanize();

            var embed = new EmbedBuilder()
                .WithTitle($"{modifyName} {GetTitle(reprimand)}")
                .WithColor(Color.Purple)
                .WithUserAsAuthor(details.User, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .WithUserAsAuthor(details.Moderator, AuthorOptions.UseFooter | AuthorOptions.Requested)
                .WithCurrentTimestamp();

            if (!string.IsNullOrWhiteSpace(reprimand.ModifiedAction?.Reason))
                embed.AddField("Reason", reprimand.ModifiedAction.Reason);

            embed
                .AddField("Total", await GetTotalAsync(reprimand, cancellationToken), true)
                .AddField("Source", reprimand.Source.Humanize(), true);

            return embed;
        }

        public async Task<EmbedBuilder> CreateEmbedAsync(ReprimandDetails details, ReprimandResult result,
            CancellationToken cancellationToken = default)
        {
            var reprimand = result.Primary;

            var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
            var options = guild.LoggingRules.Options;

            var embed = new EmbedBuilder()
                .WithUserAsAuthor(details.User, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .WithCurrentTimestamp();

            if (!options.HasFlag(LoggingOptions.Anonymous))
                embed.WithUserAsAuthor(details.Moderator, AuthorOptions.UseFooter | AuthorOptions.Requested);

            await AddPrimaryAsync(embed, details, reprimand, cancellationToken);
            foreach (var secondary in result.Secondary)
            {
                await AddSecondaryAsync(embed, details, secondary, cancellationToken);
            }

            return embed;
        }

        private async Task AddPrimaryAsync(EmbedBuilder embed, ReprimandDetails details, ReprimandAction? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                embed
                    .WithTitle(GetTitle(reprimand))
                    .WithColor(GetColor(reprimand))
                    .WithDescription(GetMessage(details, reprimand));

                if (!string.IsNullOrWhiteSpace(reprimand.Action.Reason))
                    embed.AddField("Reason", reprimand.Action.Reason);

                embed
                    .AddField("Total", await GetTotalAsync(reprimand, cancellationToken), true)
                    .AddField("Source", reprimand.Source.Humanize(), true);
            }
        }

        private async Task AddSecondaryAsync(EmbedBuilder embed, ReprimandDetails details, ReprimandAction? reprimand,
            CancellationToken cancellationToken)
        {
            if (reprimand is not null)
            {
                var total = await GetTotalAsync(reprimand, cancellationToken);
                embed
                    .WithColor(GetColor(reprimand))
                    .AddField($"{GetTitle(reprimand)} [{total}]", $"{GetMessage(details, reprimand)}");
            }
        }

        private static Color GetColor(ReprimandAction action)
        {
            return action switch
            {
                Ban     => Color.Red,
                Kick    => Color.Red,
                Mute    => Color.Orange,
                Note    => Color.Blue,
                Notice  => Color.Gold,
                Warning => Color.Gold,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };
        }

        private async ValueTask<int> GetTotalAsync(ReprimandAction action, CancellationToken cancellationToken)
        {
            var user = await action.GetUserAsync(_db, cancellationToken);

            return action switch
            {
                Ban     => user.HistoryCount<Ban>(),
                Kick    => user.HistoryCount<Kick>(),
                Mute    => user.HistoryCount<Mute>(),
                Note    => user.HistoryCount<Note>(),
                Notice  => user.HistoryCount<Notice>(),
                Warning => user.WarningCount(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };
        }

        private static string GetTitle(ReprimandAction action)
        {
            var title = action switch
            {
                Ban     => nameof(Ban),
                Kick    => nameof(Kick),
                Mute    => nameof(Mute),
                Note    => nameof(Note),
                Notice  => nameof(Notice),
                Warning => nameof(Warning),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            return $"{title.Humanize()}: {action.Id}";
        }

        private static string GetMessage(ReprimandDetails details, ReprimandAction action)
        {
            return action switch
            {
                Ban       => $"{details.User} was banned.",
                Kick      => $"{details.User} was kicked.",
                Mute m    => $"{details.User} was muted for {GetLength(m)}.",
                Note      => $"{details.User} was given a note.",
                Notice    => $"{details.User} was given a notice.",
                Warning w => $"{details.User} was warned {w.Amount} times.",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(action), action, "An unknown reprimand was given.")
            };

            static string GetLength(IMute mute)
                => mute.Length?.Humanize(5,
                    minUnit: TimeUnit.Second,
                    maxUnit: TimeUnit.Year) ?? "indefinitely";
        }
    }
}