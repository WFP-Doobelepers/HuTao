using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Image;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using static Discord.InteractionResponseType;

namespace HuTao.Services.Moderation;

public class UserService(
    AuthorizationService auth,
    IImageService image,
    InteractiveService interactive,
    HuTaoContext db)
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.History;

    public async Task ReplyAvatarAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);
        var components = await ComponentsAsync(context, user);
        var avatar = user.GetDefiniteAvatarUrl(4096);
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId)
            .WithImageUrl(avatar)
            .WithColor(await image.GetAvatarColor(user))
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        if (user is IGuildUser guild)
        {
            var guildAvatar = guild.GetGuildAvatarUrl(size: 4096);
            if (guildAvatar is not null) embed.WithThumbnailUrl(avatar).WithImageUrl(guildAvatar);
        }

        await context.ReplyAsync(embed: embed.Build(), components: components, ephemeral: ephemeral);
    }

    public async Task ReplyHistoryAsync(
        Context context, ModerationCategory? category,
        LogReprimandType type, IUser user,
        bool update, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var userEntity = await db.Users.TrackUserAsync(user, context.Guild);
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);
        category ??= userEntity.DefaultCategory ?? ModerationCategory.None;

        if (type is LogReprimandType.None)
        {
            type = category.Logging?.HistoryReprimands
                ?? guild.ModerationRules?.Logging?.HistoryReprimands
                ?? LogReprimandType.None;
        }

        var history = guild.ReprimandHistory
            .Where(r => r.UserId == user.Id)
            .OfType(type).OfCategory(category)
            .OrderByDescending(r => r.Action?.Date)
            .ToList();

        // For now, use a simple Components V2 display (will be enhanced with pagination later)
        var components = BuildHistoryComponentsV2(user, userEntity, history.Take(5).ToList(), guild, context);

        await context.ReplyAsync(components: components, ephemeral: ephemeral);
    }

    private MessageComponent BuildHistoryComponentsV2(
        IUser user, GuildUserEntity userEntity, List<Reprimand> history,
        GuildEntity guild, Context context)
    {
        // Build Components V2 interface using the new Discord.Net API
        var components = new ComponentBuilderV2()
            // User header section with "View Member" button
            .WithSection(
                [new TextDisplayBuilder($"# <@{user.Id}> History")],
                ButtonBuilder.CreateLinkButton("View Member",
                    $"discord://-/guilds/{guild.Id}/settings/members"))

            // User info container with avatar thumbnail and reprimand counts
            .WithContainer(new ContainerBuilder()
                .WithSection([
                    new TextDisplayBuilder($"<@{user.Id}> ({user.Id})"),
                    new TextDisplayBuilder(BuildUserInfoText(user, userEntity))
                ], new ThumbnailBuilder(user.GetDefiniteAvatarUrl(4096))))

            // Main separator
            .WithSeparator(new SeparatorBuilder()
                .WithSpacing(SeparatorSpacingSize.Large)
                .WithIsDivider(true));

        // Add reprimand containers or empty message
        if (!history.Any())
        {
            components.WithContainer(new ContainerBuilder()
                .WithTextDisplay("No reprimands found matching the current filter."));
        }
        else
        {
            foreach (var reprimand in history)
            {
                components.WithContainer(BuildReprimandContainer(reprimand));
            }
        }

        // Filter dropdown at bottom
        components.WithActionRow([CreateFilterSelectMenu(user)])
            // Footer
            .WithTextDisplay($"-# Requested by @{context.User.Username}");

        return components.Build();
    }

    private string BuildUserInfoText(IUser user, GuildUserEntity userEntity)
    {
        var createdText = $"Created <t:{user.CreatedAt.ToUnixTimeSeconds()}:R> <t:{user.CreatedAt.ToUnixTimeSeconds()}:f>";
        var joinedText = userEntity.JoinedAt != null
            ? $"Joined   <t:{((DateTimeOffset) userEntity.JoinedAt).ToUnixTimeSeconds()}:R> <t:{((DateTimeOffset) userEntity.JoinedAt).ToUnixTimeSeconds()}:f>"
            : "Joined   Unknown";

        // Build reprimand counts similar to the Python example
        var reprimandCounts = new List<string>();

        // Get counts for each reprimand type
        var warningCount = userEntity.WarningCount(null);
        var noticeCount = userEntity.HistoryCount<Notice>(null);
        var banCount = userEntity.HistoryCount<Ban>(null);
        var kickCount = userEntity.HistoryCount<Kick>(null);
        var noteCount = userEntity.HistoryCount<Note>(null);
        var muteCount = userEntity.HistoryCount<Mute>(null);
        var censoredCount = userEntity.HistoryCount<Censored>(null);

        reprimandCounts.Add($"-# - Warning {warningCount.Active}/{warningCount.Total} [{warningCount.Active}/{warningCount.Total}]");
        reprimandCounts.Add($"-# - Notice {noticeCount.Active}/{noticeCount.Total} [{noticeCount.Active}/{noticeCount.Total}]");
        reprimandCounts.Add($"-# - Ban {banCount.Active}/{banCount.Total} [{banCount.Active}/{banCount.Total}]");
        reprimandCounts.Add($"-# - Kick {kickCount.Active}/{kickCount.Total} [{kickCount.Active}/{kickCount.Total}]");
        reprimandCounts.Add($"-# - Note {noteCount.Active}/{noteCount.Total} [{noteCount.Active}/{noteCount.Total}]");
        reprimandCounts.Add($"-# - Mute {muteCount.Active}/{muteCount.Total} [{muteCount.Active}/{muteCount.Total}]");
        reprimandCounts.Add($"-# - Censored {censoredCount.Active}/{censoredCount.Total} [{censoredCount.Active}/{censoredCount.Total}]");

        return $"{createdText}\n{joinedText}\n\n{string.Join("\n", reprimandCounts)}";
    }

    private ContainerBuilder BuildReprimandContainer(Reprimand reprimand)
    {
        var container = new ContainerBuilder();

        // Get reprimand ID (first 8 characters for display)
        var shortId = reprimand.Id.ToString()[..8];
        var reprimandType = GetReprimandDisplayName(reprimand);

        // Reprimand header section with edit button
        container.WithSection(
            [new TextDisplayBuilder($"### {reprimandType} • [{shortId}]\n-# <@{reprimand.Action?.Moderator?.Id ?? 0}> <t:{((DateTimeOffset?) reprimand.Action?.Date)?.ToUnixTimeSeconds() ?? 0}:d> <t:{((DateTimeOffset?) reprimand.Action?.Date)?.ToUnixTimeSeconds() ?? 0}:t> • <t:{((DateTimeOffset?) reprimand.Action?.Date)?.ToUnixTimeSeconds() ?? 0}:R>\n{reprimand.Action?.Reason ?? "No reason provided"}")],
            new ButtonBuilder("", $"hist-edit:{reprimand.Id}", ButtonStyle.Secondary,
                emote: new Emoji("✏️")));

        // Add separator for additional notes if they exist
        if (!string.IsNullOrEmpty(reprimand.Action?.AdditionalInformation))
        {
            container.WithSeparator(new SeparatorBuilder()
                .WithSpacing(SeparatorSpacingSize.Small)
                .WithIsDivider(true));

            container.WithTextDisplay($"-# {reprimand.Action.AdditionalInformation}");
        }

        // Add media gallery if there are attachments (placeholder for now)
        // This would need to be implemented based on how attachments are stored in your system
        // container.WithMediaGallery(BuildMediaGallery(reprimand));

        // Add action dropdown for this reprimand
        container.WithActionRow([
            new SelectMenuBuilder($"hist-action:{reprimand.Id}", [
                new SelectMenuOptionBuilder("Forgive", $"forgive-{reprimand.Id}", emoji: new Emoji("➖")),
                new SelectMenuOptionBuilder("Delete", $"delete-{reprimand.Id}", emoji: new Emoji("🗑️"))
            ])
            .WithPlaceholder("Action...")
            .WithMinValues(1)
            .WithMaxValues(1)
        ]);

        return container;
    }



    private string GetReprimandDisplayName(Reprimand reprimand)
    {
        var typeName = reprimand.GetType().Name;
        // TODO: Add counting logic to show "3rd Warning", "2nd Ban", etc.
        return typeName;
    }

    private SelectMenuBuilder CreateFilterSelectMenu(IUser user)
    {
        return new SelectMenuBuilder("hist-filter", [
            new SelectMenuOptionBuilder("All", "all"),
            new SelectMenuOptionBuilder("Ban", "ban"),
            new SelectMenuOptionBuilder("Kick", "kick"),
            new SelectMenuOptionBuilder("Mute", "mute"),
            new SelectMenuOptionBuilder("Warning", "warning"),
            new SelectMenuOptionBuilder("Note", "note")
        ])
        .WithPlaceholder("Filter...")
        .WithMinValues(1)
        .WithMaxValues(6);
    }





    public async Task ReplyUserAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var components = await ComponentsAsync(context, user);
        var builders = await GetUserAsync(context, user);
        var embeds = builders.Select(e => e.Build()).ToArray();

        await context.ReplyAsync(components: components, embeds: embeds, ephemeral: ephemeral);
    }

    private static EmbedBuilder GetReprimands(GuildUserEntity user, ModerationCategory? category)
    {
        var rules = category?.Logging?.SummaryReprimands
            ?? user.Guild.ModerationRules?.Logging?.SummaryReprimands
            ?? LogReprimandType.All;

        var embed = new EmbedBuilder().WithTitle(category == ModerationCategory.All
            ? "Reprimands [Active/Total]"
            : "Reprimands [Active/Total] [Global]");

        if (rules.HasFlag(LogReprimandType.Warning))
            embed.AddField(Warnings(user, category));

        if (rules.HasFlag(LogReprimandType.Notice))
            embed.AddField(Reprimands<Notice>(user, category));

        if (rules.HasFlag(LogReprimandType.Ban))
            embed.AddField(Reprimands<Ban>(user, category));

        if (rules.HasFlag(LogReprimandType.Kick))
            embed.AddField(Reprimands<Kick>(user, category));

        if (rules.HasFlag(LogReprimandType.Note))
            embed.AddField(Reprimands<Note>(user, category));

        if (rules.HasFlag(LogReprimandType.Mute))
            embed.AddField(Reprimands<Mute>(user, category));

        if (rules.HasFlag(LogReprimandType.Censored))
            embed.AddField(Reprimands<Censored>(user, category));

        return embed;
    }

    private static EmbedFieldBuilder Reprimands<T>(GuildUserEntity user, ModerationCategory? category)
        where T : Reprimand
    {
        var global = user.HistoryCount<T>(null);
        var count = user.HistoryCount<T>(category);

        var embed = new EmbedFieldBuilder()
            .WithName(typeof(T).Name)
            .WithIsInline(true);

        return category is null
            ? embed.WithValue($"{count.Active}/{count.Total}")
            : embed.WithValue($"{count.Active}/{count.Total} [{global.Active}/{global.Total}]");
    }

    private static EmbedFieldBuilder Warnings(GuildUserEntity user, ModerationCategory? category)
    {
        var global = user.WarningCount(null);
        var count = user.WarningCount(category);

        var embed = new EmbedFieldBuilder()
            .WithName(nameof(Warning))
            .WithIsInline(true);

        return category is null
            ? embed.WithValue($"{count.Active}/{count.Total}")
            : embed.WithValue($"{count.Active}/{count.Total} [{global.Active}/{global.Total}]");
    }

    private static SelectMenuBuilder CategoryMenu(
        IUser user, IEnumerable<ModerationCategory> categories,
        ModerationCategory? selected, LogReprimandType type = LogReprimandType.None)
        => new SelectMenuBuilder()
            .WithCustomId($"category:{user.Id}:{(int) type}")
            .WithPlaceholder("Select a category")
            .WithMinValues(0).WithMaxValues(1)
            .WithOptions(categories
                .Prepend(ModerationCategory.None)
                .Append(ModerationCategory.All)
                .Select(c => new SelectMenuOptionBuilder(
                    c.Name, c.Name, isDefault: selected?.Name == c.Name))
                .ToList());

    private static SelectMenuBuilder HistoryMenu(
        IUser user,
        ModerationCategory? category = null, LogReprimandType type = LogReprimandType.None)
    {
        var types = Enum.GetValues<LogReprimandType>()[1..^1];

        var menu = new SelectMenuBuilder()
            .WithCustomId($"reprimand:{user.Id}:{category?.Name}")
            .WithPlaceholder("View History")
            .WithMinValues(1).WithMaxValues(types.Length);

        foreach (var e in types)
        {
            var name = e.ToString();
            var title = e.Humanize(LetterCasing.Title);
            var selected = type.HasFlag(e) && type is not LogReprimandType.None;
            menu.AddOption(title, name, $"View {title} history", isDefault: selected);
        }

        return menu;
    }

    private static SelectMenuBuilder ReprimandMenu(IUser user)
    {
        var menu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithCustomId($"mod-menu:{user.Id}")
            .WithPlaceholder("Mod Menu")
            .AddOption("Ban", nameof(LogReprimandType.Ban), "Ban the user")
            .AddOption("Note", nameof(LogReprimandType.Note), "Add a note to the user");

        if (user is IGuildUser)
        {
            menu.AddOption("Warn", nameof(LogReprimandType.Warning), "Warn the user")
                .AddOption("Kick", nameof(LogReprimandType.Kick), "Kick the user")
                .AddOption("Mute", nameof(LogReprimandType.Mute), "Mute the user")
                .AddOption("Hard Mute", nameof(LogReprimandType.HardMute), "Hard Mute the user");
        }

        return menu;
    }

    private async Task<IEnumerable<EmbedBuilder>> GetUserAsync(Context context, IUser user)
    {
        var isAuthorized =
            await auth.IsAuthorizedAsync(context, Scope) ||
            await auth.IsCategoryAuthorizedAsync(context, Scope);

        var userEntity = await db.Users.FindAsync(user.Id, context.Guild.Id);
        var guildUser = user as SocketGuildUser;

        var embeds = new List<EmbedBuilder>();
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
            .WithDescription(user.Mention)
            .AddField("Created", user.CreatedAt.ToUniversalTimestamp());
        embeds.Add(embed);

        if (userEntity?.JoinedAt is not null)
            embed.AddField("First Joined", userEntity.JoinedAt.Value.ToUniversalTimestamp());

        if (guildUser is not null)
        {
            if (guildUser.JoinedAt is not null)
                embed.AddField("Joined", guildUser.JoinedAt.Value.ToUniversalTimestamp());

            var roles = guildUser.Roles
                .OrderByDescending(r => r.Position)
                .ToList();

            embed
                .WithColor(roles.Select(r => r.Color).FirstOrDefault(c => c.RawValue is not 0))
                .AddItemsIntoFields($"Roles [{guildUser.Roles.Count}]", roles.Select(r => r.Mention), " ");

            if (isAuthorized)
            {
                if (guildUser.TimedOutUntil is not null)
                    embed.AddField("Timeout", guildUser.TimedOutUntil.Humanize());

                var mute = await db.GetActive<Mute>(guildUser);
                if (mute is not null) embed.AddField("Muted", mute.ExpireAt.Humanize(), true);
            }
        }

        var ban = await context.Guild.GetBanAsync(user);
        if (!isAuthorized || ban is null) return embeds;

        embed.WithColor(Color.Red);
        var banDetails = userEntity?.Reprimands<Ban>(null).MaxBy(b => b.Action?.Date);
        if (banDetails is not null)
            embeds.Add(banDetails.ToEmbedBuilder(true));
        else
            embed.AddField("Banned", $"This user is banned. Reason: {ban.Reason ?? "None"}");

        return embeds;
    }

    private async Task<MessageComponent?> ComponentsAsync(Context context, IUser user)
    {
        var auth1 = await auth.IsAuthorizedAsync(context, Scope);
        var category = await auth.IsCategoryAuthorizedAsync(context, Scope);
        return auth1 || category
            ? new ComponentBuilder()
                .WithSelectMenu(HistoryMenu(user))
                .WithSelectMenu(ReprimandMenu(user))
                .Build()
            : null;
    }
}