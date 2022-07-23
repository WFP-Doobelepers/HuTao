using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogChannelConfig;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig.ModerationLogOptions;

namespace HuTao.Services.Moderation;

public record LogConfig<T>(T? Config, T Template) where T : ModerationLogConfig
{
    public LogReprimandStatus LogReprimandStatus
        => Config?.LogReprimandStatus ?? Template.LogReprimandStatus ?? LogReprimandStatus.All;

    public LogReprimandType LogReprimands
        => Config?.LogReprimands ?? Template.LogReprimands ?? LogReprimandType.All;

    public LogReprimandType ShowAppealOnReprimands
        => Config?.ShowAppealOnReprimands ?? Template.ShowAppealOnReprimands ?? LogReprimandType.All;

    public ModerationLogOptions Options => Config?.Options ?? Template.Options ?? All;

    public string MentionChannel
        => Config is ModerationLogChannelConfig config ? config.MentionChannel() : "Not configured";

    public string? AppealMessage => Config?.AppealMessage ?? Template.AppealMessage;

    public ulong ChannelId => Config is ModerationLogChannelConfig config ? config.ChannelId : default;
}

public class ModerationLoggingService
{
    private readonly HuTaoContext _db;

    public ModerationLoggingService(HuTaoContext db) { _db = db; }

    public async Task<ReprimandResult> PublishReprimandAsync(ReprimandResult result, ReprimandDetails details,
        CancellationToken cancellationToken = default)
    {
        var reprimand = result.Last;
        var buttons = reprimand.ToComponentBuilder().Build();
        var guild = await reprimand.GetGuildAsync(_db, cancellationToken);

        var commandLog = GetConfig(r => r?.CommandLog, DefaultCommandLogConfig);
        var userLog = GetConfig(r => r?.UserLog, DefaultUserLogConfig);

        var published = false;
        if (reprimand.IsIncluded(commandLog) && details.Context is not null)
            published = await PublishToContextAsync(details.Context, commandLog);

        if (reprimand.IsIncluded(userLog))
            await PublishToUserAsync(details.User, userLog);

        await PublishAsync(GetConfig(r => r?.ModeratorLog, DefaultModeratorLogConfig));
        await PublishAsync(GetConfig(r => r?.PublicLog, DefaultPublicLogConfig));

        return result;

        LogConfig<T> GetConfig<T>(Func<ModerationLoggingRules?, T?> selector, T template) where T : ModerationLogConfig
            => reprimand.Category is null
                ? new LogConfig<T>(selector(guild.ModerationRules?.Logging), template)
                : new LogConfig<T>(selector(reprimand.Category.Logging), template);

        async Task PublishAsync(LogConfig<ModerationLogChannelConfig> config)
        {
            if (config.Config is null) return;
            if (!reprimand.IsIncluded(config)) return;
            if (published
                && (details.Category?.Logging?.IgnoreDuplicates ??
                    guild.ModerationRules?.Logging?.IgnoreDuplicates ?? false)
                && details.Context?.Channel.Id == config.ChannelId) return;

            var text = await details.Guild.GetTextChannelAsync(config.ChannelId);
            await PublishToChannelAsync(text, config, buttons);
        }

        async Task<bool> PublishToContextAsync(Context context, LogConfig<ModerationLogConfig> config)
        {
            var embed = await CreateEmbedAsync(result, details, config, cancellationToken);
            try
            {
                if (context is InteractionContext
                    {
                        Interaction: IComponentInteraction or IModalInteraction
                    } interaction)
                {
                    var modalComponents = reprimand.ToComponentBuilder(details.Ephemeral).Build();
                    if (details.Modify)
                    {
                        await interaction.ModifyOriginalResponseAsync(m =>
                        {
                            m.Embed      = embed.Build();
                            m.Components = modalComponents;
                        });
                    }
                    else
                    {
                        await interaction.FollowupAsync(
                            embed: embed.Build(),
                            components: modalComponents,
                            ephemeral: details.Ephemeral);
                    }

                    return !details.Ephemeral;
                }

                await context.ReplyAsync(embed: embed.Build(), ephemeral: details.Ephemeral, components: buttons);
                return !(context is InteractionContext && details.Ephemeral);
            }
            catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
            {
                await context.ReplyAsync(new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {context.User}.")
                    .AppendLine(e.Message).ToString(), ephemeral: true);
                return false;
            }
        }

        async Task PublishToUserAsync(IUser user, LogConfig<ModerationLogConfig> config)
        {
            try
            {
                await PublishToChannelAsync(await user.CreateDMChannelAsync(), config);
            }
            catch (HttpException e)
            {
                if (details.Context is null) return;
                await details.Context.ReplyAsync(new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {user}.")
                    .AppendLine(e.Message).ToString(), ephemeral: true);
            }
        }

        async Task PublishToChannelAsync<T>(
            IMessageChannel? channel, LogConfig<T> config,
            MessageComponent? components = null) where T : ModerationLogConfig
        {
            try
            {
                if (channel is null) return;
                var embed = await CreateEmbedAsync(result, details, config, cancellationToken);
                await channel.SendMessageAsync(embed: embed.Build(), components: components);
            }
            catch (HttpException e) when (e.HttpCode is HttpStatusCode.Forbidden)
            {
                if (details.Context is null) return;
                await details.Context.ReplyAsync(new StringBuilder()
                    .AppendLine($"Could not publish reprimand for {channel}.")
                    .AppendLine(e.Message).ToString(), ephemeral: true);
            }
        }
    }

    private async Task AddPrimaryAsync(
        EmbedBuilder embed, Reprimand reprimand, ReprimandDetails details,
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

        var count = await reprimand.CountUserReprimandsAsync(_db, cancellationToken);
        if (options.HasFlag(ShowActive)) embed.AddField("Active", count.Active, true);
        if (options.HasFlag(ShowTotal)) embed.AddField("Total", count.Total, true);

        if (options.HasFlag(ShowCategory))
            embed.AddField("Category", reprimand.Category?.Name ?? "None", true);

        if (options.HasFlag(ShowTrigger))
        {
            var trigger = await reprimand.GetTriggerAsync<Trigger>(_db, cancellationToken);
            if (trigger is not null)
            {
                embed
                    .AddField("Trigger", trigger.GetDetails(), true)
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
            var count = await secondary.CountUserReprimandsAsync(_db, cancellationToken);
            embed.AddField($"{secondary.GetTitle(showId)} [{count.Active}/{count.Total}]", message);
        }
        else
            embed.AddField($"{secondary.GetTitle(showId)}", message);
    }

    private async Task<EmbedBuilder> CreateEmbedAsync<T>(
        ReprimandResult result, ReprimandDetails details, LogConfig<T> config,
        CancellationToken cancellationToken = default) where T : ModerationLogConfig
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