using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
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

        if (reprimand.IsIncluded(options.CommandLog) && details.Context is not null)
        {
            if (details.Context is InteractionContext interaction)
                await PublishToInteractionAsync(options.CommandLog, interaction);
            else
                await PublishToChannelAsync(options.CommandLog, details.Context.Channel);
        }

        if (reprimand.IsIncluded(options.UserLog))
            await PublishToUserAsync(options.UserLog, details.User);

        async Task PublishAsync<T>(T config) where T : ModerationLogConfig, IChannelEntity
        {
            if (!reprimand.IsIncluded(config)) return;

            var channel = await details.Guild.GetTextChannelAsync(config.ChannelId);
            await PublishToChannelAsync(config, channel);
        }

        async Task PublishToInteractionAsync(ModerationLogConfig config, InteractionContext context)
        {
            var embed = await CreateEmbedAsync(result, details, config, cancellationToken);
            try
            {
                await context.RespondAsync(embed: embed.Build(), ephemeral: true);
            }
            catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
            {
                if (details.Context is null) return;
                var message = new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {context.User}.")
                    .AppendLine(e.Message);

                await context.RespondAsync(message.ToString(), ephemeral: true);
            }
        }

        async Task PublishToUserAsync(ModerationLogConfig config, IUser user)
        {
            try
            {
                await PublishToChannelAsync(config, await user.CreateDMChannelAsync());
            }
            catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
            {
                if (details.Context is not CommandContext context) return;
                var message = new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {user}.")
                    .AppendLine(e.Message);

                await _error.AssociateError(context.Message, message.ToString());
            }
        }

        async Task PublishToChannelAsync(ModerationLogConfig config, IMessageChannel? channel)
        {
            if (channel is null) return;
            try
            {
                var embed = await CreateEmbedAsync(result, details, config, cancellationToken);
                await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
            {
                if (details.Context is not CommandContext context) return;
                var message = new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {channel}.")
                    .AppendLine(e.Message);

                await _error.AssociateError(context.Message, message.ToString());
            }
        }

        return result;
    }

    private async Task AddPrimaryAsync(EmbedBuilder embed, Reprimand reprimand, ReprimandDetails details,
        ModerationLogOptions options, CancellationToken cancellationToken)
    {
        AddReprimandUser(details.User);
        AddReprimandModerator(details.Moderator);

        if (options.HasFlag(ShowDetails))
            embed.WithDescription(reprimand.GetAction());

        var reason = reprimand.ModifiedAction?.Reason ?? reprimand.Action?.Reason;
        if (options.HasFlag(ShowReason) && !string.IsNullOrWhiteSpace(reason))
        {
            var reasons = reason.Split(" ");
            embed.AddItemsIntoFields("Reason", reasons.ToArray(), " ");
        }

        if (options.HasFlag(ShowActive))
            embed.AddField("Active", await reprimand.GetTotalAsync(_db, false, cancellationToken), true);

        if (options.HasFlag(ShowTotal))
            embed.AddField("Total", await reprimand.GetTotalAsync(_db, true, cancellationToken), true);

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

    private async Task AddSecondaryAsync(EmbedBuilder embed, Reprimand secondary, ModerationLogOptions options,
        CancellationToken cancellationToken)
    {
        embed.WithColor(secondary.GetColor());

        var showId = options.HasFlag(ShowReprimandId);
        var message = secondary.GetAction();

        if (options.HasFlag(ShowActive))
        {
            var active = await secondary.GetTotalAsync(_db, false, cancellationToken);
            var total = await secondary.GetTotalAsync(_db, true, cancellationToken);

            embed.AddField($"{secondary.GetTitle(showId)} [{active}/{total}]", message);
        }
        else
            embed.AddField($"{secondary.GetTitle(showId)}", message);
    }

    private async Task<EmbedBuilder> CreateEmbedAsync(ReprimandResult result, ReprimandDetails details,
        ModerationLogConfig config,
        CancellationToken cancellationToken = default)
    {
        var title = result.Primary.GetTitle(config.Options.HasFlag(ShowReprimandId));
        var embed = new EmbedBuilder()
            .WithCurrentTimestamp()
            .WithTitle($"{result.Primary.Status.Humanize()} {title}")
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
}