using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Utilities;
using static Zhongli.Data.Models.Moderation.Logging.ModerationLogConfig;
using static Zhongli.Data.Models.Moderation.Logging.ModerationLogConfig.ModerationLogOptions;

namespace Zhongli.Services.Moderation;

public class ModerationLoggingService
{
    private readonly CommandErrorHandler _error;
    private readonly ZhongliContext _db;

    public ModerationLoggingService(CommandErrorHandler error, ZhongliContext db)
    {
        _error = error;
        _db    = db;
    }

    public async Task<ReprimandResult> PublishReprimandAsync(ReprimandResult result, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var reprimand = result.Last;
        var guild = await reprimand.GetGuildAsync(_db, cancellationToken);
        var options = guild.ModerationLoggingRules;

        await PublishAsync(options.ModeratorLog);
        await PublishAsync(options.PublicLog);

        if (reprimand.IsIncluded(options.CommandLog))
            await PublishToChannelAsync(options.CommandLog, details.Context?.Channel);

        if (reprimand.IsIncluded(options.UserLog))
            await PublishToChannelAsync(options.UserLog, await details.User.CreateDMChannelAsync());

        async Task PublishAsync<T>(T config) where T : ModerationLogConfig, IChannelEntity
        {
            if (!reprimand.IsIncluded(config)) return;

            var channel = await details.Guild.GetTextChannelAsync(config.ChannelId);
            await PublishToChannelAsync(config, channel);
        }

        async Task PublishToChannelAsync(ModerationLogConfig config, IMessageChannel? channel)
        {
            if (channel is null) return;
            var embed = await CreateEmbedAsync(result, details, config, cancellationToken);
            try
            {
                await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                if (details.Context is null) return;
                var message = new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {channel}.")
                    .AppendLine(e.Message);

                _ = _error.AssociateError(details.Context.Message, message.ToString());
            }
        }

        return result;
    }

    private async Task AddPrimaryAsync(EmbedBuilder embed, Reprimand reprimand, ReprimandDetails details, ModerationLogOptions options, CancellationToken cancellationToken)
    {
        AddReprimandUser(details.User);
        AddReprimandModerator(details.Moderator);

        if (options.HasFlag(ShowDetails))
            embed.WithDescription(reprimand.GetMessage());

        var reason = reprimand.ModifiedAction?.Reason ?? reprimand.Action?.Reason;
        if (options.HasFlag(ShowReason) && !string.IsNullOrWhiteSpace(reason))
        {
            var reasons = reason.Split(" ");
            embed.AddItemsIntoFields("Reason", reasons.ToArray(), " ");
        }

        if (options.HasFlag(ShowActive))
            embed.AddField("Active", await GetTotalAsync(reprimand, false, cancellationToken), true);

        if (options.HasFlag(ShowTotal))
            embed.AddField("Total", await GetTotalAsync(reprimand, true, cancellationToken), true);

        if (options.HasFlag(ShowTrigger))
        {
            var trigger = await reprimand.GetTriggerAsync<Trigger>(_db, cancellationToken);
            if (trigger is not null)
            {
                embed
                    .AddField("Trigger", trigger.GetTriggerDetails(), true)
                    .AddField("Trigger ID", trigger.Id, true);
            }
        }

        void AddReprimandModerator(IGuildUser moderator)
        {
            const AuthorOptions author = AuthorOptions.UseFooter | AuthorOptions.Requested;
            if (options.HasFlag(ShowModerator))
                embed.WithUserAsAuthor(moderator, author | AuthorOptions.IncludeId);
            else
                embed.WithGuildAsAuthor(moderator.Guild, author);
        }

        void AddReprimandUser(IUser user)
        {
            if (!options.HasFlag(ShowUser)) return;

            var author = AuthorOptions.IncludeId;
            if (options.HasFlag(ShowAvatarThumbnail))
                author |= AuthorOptions.UseThumbnail;

            embed.WithUserAsAuthor(user, author);
        }
    }

    private async Task AddSecondaryAsync(EmbedBuilder embed, Reprimand secondary, ModerationLogOptions options, CancellationToken cancellationToken)
    {
        embed.WithColor(secondary.GetColor());

        var showId = options.HasFlag(ShowReprimandId);
        var message = secondary.GetMessage();

        if (options.HasFlag(ShowActive))
        {
            var active = await GetTotalAsync(secondary, false, cancellationToken);
            var total = await GetTotalAsync(secondary, true, cancellationToken);

            embed.AddField($"{secondary.GetTitle(showId)} [{active}/{total}]", message);
        }
        else
            embed.AddField($"{secondary.GetTitle(showId)}", message);
    }

    private async Task<EmbedBuilder> CreateEmbedAsync(ReprimandResult result, ReprimandDetails details, ModerationLogConfig config,
        CancellationToken cancellationToken = default)
    {
        var embed = new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithTitle($"{result.Primary.Status.Humanize()} {result.Primary.GetTitle(config.Options.HasFlag(ShowReprimandId))}")
            .WithColor(result.Primary.GetColor());

        var showAppeal = result.Primary.IsIncluded(config.ShowAppealOnReprimands);
        await AddPrimaryAsync(embed, result.Primary, details, config.Options, cancellationToken);
        foreach (var secondary in result.Secondary)
        {
            await AddSecondaryAsync(embed, secondary, config.Options, cancellationToken);
            showAppeal = showAppeal || secondary.IsIncluded(config.ShowAppealOnReprimands);
        }
        if (showAppeal && !string.IsNullOrWhiteSpace(config.AppealMessage))
            embed.AddField("Appeal", config.AppealMessage);

        return embed;
    }

    private async ValueTask<uint> GetTotalAsync(Reprimand reprimand, bool countHidden = true,
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
}