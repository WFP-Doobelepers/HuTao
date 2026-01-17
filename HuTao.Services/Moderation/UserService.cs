using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
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
    AuthorizationService authService,
    IImageService image,
    IReprimandHistoryImageService historyImage,
    InteractiveService interactive,
    HuTaoContext db)
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.History;

    public async Task ReplyAvatarAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var avatar = user.GetDefiniteAvatarUrl(4096);
        var accentColor = (await image.GetAvatarColor(user)).RawValue;

        var media = new List<MediaGalleryItemProperties>();
        if (user is IGuildUser guild)
        {
            var guildAvatar = guild.GetGuildAvatarUrl(size: 4096);
            if (!string.IsNullOrWhiteSpace(guildAvatar))
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(guildAvatar), "Guild avatar"));
        }

        media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(avatar), "User avatar"));

        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Avatar\n**User:** {user} ({user.Mention})")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(user.GetDisplayAvatarUrl(size: 256) ?? avatar)))
            .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
            .WithMediaGallery(media)
            .WithAccentColor(accentColor);

        var builder = new ComponentBuilderV2()
            .WithContainer(container);

        await AddUserMenusAsync(builder, context, user);

        await context.ReplyAsync(
            components: builder.Build(),
            ephemeral: ephemeral,
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// Displays user history using ComponentPaginator V2 with enhanced filtering and image support.
    /// Works with both text commands and slash commands.
    /// </summary>
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
            .Where(r => r.UserId == user.Id && r.Status is not ReprimandStatus.Deleted)
            .OrderByDescending(r => r.Action?.Date)
            .ToList();

        using var imageStream = await historyImage.GenerateHistoryImageAsync(userEntity, category);
        var imageBytes = imageStream.ToArray();

        var state = new UserHistoryPaginatorState(user, userEntity, history, category, type, guild, context.User, imageBytes);
        var paginator = new ComponentPaginatorBuilder()
            .WithUsers(context.User)
            .WithPageFactory(p => GenerateUserHistoryPage(p, state))
            .WithPageCount(state.TotalPages)
            .WithUserState(state)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithActionOnCancellation(ActionOnStop.DisableInput)
            .Build();

        await (context switch
        {
            CommandContext command => interactive.SendPaginatorAsync(
                paginator, command.Channel,
                timeout: TimeSpan.FromMinutes(15),
                resetTimeoutOnInput: true),

            InteractionContext { Interaction: SocketInteraction interaction }
                => interactive.SendPaginatorAsync(paginator, interaction,
                    ephemeral: ephemeral,
                    responseType: update ? DeferredUpdateMessage : DeferredChannelMessageWithSource,
                    timeout: TimeSpan.FromMinutes(15),
                    resetTimeoutOnInput: true),

            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context, "Invalid context.")
        });
    }

    public const string SelectOptionId = "select_option";
    private const int MaxTextDisplayLength = 4000;

    /// <summary>
    /// Renders reprimands grouped by type with full reasons and moderator info.
    /// Categories are sorted by their latest reprimand date.
    /// Duplicate reasons are auto-collapsed.
    /// </summary>
    private static void RenderGroupedReprimands(ComponentBuilderV2 components, 
        IEnumerable<Reprimand> reprimands, int availableComponents)
    {
        var reprimandList = reprimands.ToList();
        
        const int maxTotalMessageText = 3500;
        
        var container = new ContainerBuilder();
        var componentCount = 1; // Start at 1 for the container itself
        var totalMessageTextLength = 0;
        
        var grouped = reprimandList
            .GroupBy(r => r.GetTitle(showId: false))
            .OrderByDescending(g => g.Max(r => r.Action?.Date))
            .ToList();
        
        foreach (var group in grouped)
        {
            // TextDisplay = 1 component
            if (componentCount + 1 > availableComponents)
                break;
            
            var entries = group.OrderByDescending(r => r.Action?.Date).ToList();
            
            var firstEntry = entries.First();
            var typeBadges = firstEntry.GetTitle(showId: false);
            
            var groupText = new StringBuilder();
            groupText.AppendLine($"### {typeBadges}");
            var headerLength = groupText.Length;
            
            if (totalMessageTextLength + headerLength > maxTotalMessageText && componentCount > 1)
                break;
            
            var collapsedEntries = CollapseIdenticalReasons(entries);
            
            foreach (var (reprimandOrGroup, count) in collapsedEntries)
            {
                var reason = reprimandOrGroup.Action?.Reason ?? "No reason provided";
                var date = reprimandOrGroup.Action?.Date ?? DateTimeOffset.UtcNow;
                var moderator = reprimandOrGroup.Action?.Moderator is { } mod ? $"<@{mod.Id}>" : "System";
                var dateStr = $"<t:{date.ToUnixTimeSeconds()}:d>";
                var timeStr = $"<t:{date.ToUnixTimeSeconds()}:t>";
                var relativeStr = $"<t:{date.ToUnixTimeSeconds()}:R>";
                
                var countPrefix = count > 1 ? $"**x{count}** " : "";
                var entry = $"• {countPrefix}{reason}\n-# {moderator} {dateStr} {timeStr} • {relativeStr}\n";
                
                if (totalMessageTextLength + groupText.Length + entry.Length > maxTotalMessageText)
                    break;
                
                groupText.Append(entry);
            }
            
            if (groupText.Length > headerLength)
            {
                container.WithTextDisplay(groupText.ToString().TrimEnd().Truncate(MaxTextDisplayLength));
                componentCount++;
                totalMessageTextLength += groupText.Length;
            }
        }
        
        if (componentCount > 1) // More than just the container
        {
            components.WithContainer(container);
        }
    }
    
    /// <summary>
    /// Collapses reprimands with identical reasons into groups with counts.
    /// Returns tuples of (representative reprimand, count), preserving order by using the first occurrence.
    /// </summary>
    private static IEnumerable<(Reprimand Reprimand, int Count)> CollapseIdenticalReasons(IEnumerable<Reprimand> reprimands)
        => reprimands
            .GroupBy(r => r.Action?.Reason ?? "")
            .Select(g => (Reprimand: g.First(), Count: g.Count()));

    /// <summary>
    /// Page factory method for Components V2 user history paginator
    /// </summary>
    private static IPage GenerateUserHistoryPage(IComponentPaginator p, UserHistoryPaginatorState state)
    {
        const int maxTotalComponents = 40;
        
        var currentReprimands = state.GetReprimandsForPage(p.CurrentPageIndex).ToList();
        var components = new ComponentBuilderV2();
        var usedComponents = 0;
        
        var showHistoryImage = true;
        var createdTimestamp = $"<t:{state.User.CreatedAt.ToUnixTimeSeconds()}:R> <t:{state.User.CreatedAt.ToUnixTimeSeconds()}:f>";
        var joinedTimestamp = state.UserEntity.JoinedAt != null 
            ? $"<t:{state.UserEntity.JoinedAt.Value.ToUnixTimeSeconds()}:R> <t:{state.UserEntity.JoinedAt.Value.ToUnixTimeSeconds()}:f>"
            : "Unknown";
        
        if (p.CurrentPageIndex == 0)
        {
            components.WithTextDisplay($"# {state.User.Mention}'s History\n\n" +
                                      $"-# Created {createdTimestamp}\n" +
                                      $"-# Joined   {joinedTimestamp}");
            usedComponents++; // TextDisplay
            
            if (showHistoryImage)
            {
                var historyImageContainer = new ContainerBuilder()
                    .WithMediaGallery([new MediaGalleryItemProperties(
                        new UnfurledMediaItemProperties("attachment://reprimand_history.png"))]);
                
                components.WithContainer(historyImageContainer);
                usedComponents += 2; // Container + MediaGallery inside
            }
            
            components.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
            usedComponents++; // Separator
        }
        else
        {
            var headerText = $"# {state.User.Mention} History\n\n" +
                             $"-# Total Records: {state.TotalReprimands} • Filter: {state.TypeFilter.Humanize()}";
            
            components.WithTextDisplay(headerText);
            usedComponents++; // TextDisplay
            
            components.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
            usedComponents++; // Separator
        }

        // Reserve components for footer elements:
        // - 4-5 ActionRows (type filter, category filter if exists, mod actions, navigation)
        // - 1 TextDisplay (footer)
        var hasCategories = state.Guild.ModerationCategories.Any();
        var reservedForFooter = (hasCategories ? 5 : 4) + 1; // ActionRows + footer TextDisplay
        var availableForContent = maxTotalComponents - usedComponents - reservedForFooter;

        // Display reprimands or empty message
        if (!currentReprimands.Any())
        {
            components.WithTextDisplay("*No reprimands found matching your criteria.*\n\n" +
                                      "This user has a clean record with the current filters applied.");
        }
        else
        {
            RenderGroupedReprimands(components, currentReprimands, availableForContent);
        }

        // Build reprimand type filter select menu (outside container, in top-level action row)
        var types = Enum.GetValues<LogReprimandType>()[1..^1];
        var typeOptions = types.Select(t =>
        {
            var name = t.ToString();
            var title = t.Humanize(LetterCasing.Title);
            var selected = state.TypeFilter.HasFlag(t) && state.TypeFilter is not LogReprimandType.None;
            return new SelectMenuOptionBuilder(title, name, isDefault: selected);
        }).ToList();

        components.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu($"reprimand:{state.UserEntity.Id}:{state.CategoryFilter?.Name ?? "None"}", 
                typeOptions, "Filter...", 0, types.Length, disabled: p.ShouldDisable()));

        // Category filter if available (outside container)
        if (state.Guild.ModerationCategories.Any())
        {
            var categoryOptions = state.Guild.ModerationCategories
                .Select(c => new SelectMenuOptionBuilder(c.Name.Truncate(SelectMenuOptionBuilder.MaxSelectLabelLength), 
                    c.Id.ToString(),
                    isDefault: c.Id == state.CategoryFilter?.Id))
                .Prepend(new SelectMenuOptionBuilder("All Categories", "all",
                    isDefault: state.CategoryFilter == null))
                .ToList();

            components.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu("history-category-filter", categoryOptions,
                    "Filter by category...", disabled: p.ShouldDisable()));
        }

        // Moderation actions menu (outside container)
        var modActions = new List<SelectMenuOptionBuilder>
        {
            new SelectMenuOptionBuilder("Ban", nameof(LogReprimandType.Ban), "Ban the user"),
            new SelectMenuOptionBuilder("Note", nameof(LogReprimandType.Note), "Add a note to the user")
        };

        if (state.User is IGuildUser)
        {
            modActions.Add(new SelectMenuOptionBuilder("Warn", nameof(LogReprimandType.Warning), "Warn the user"));
            modActions.Add(new SelectMenuOptionBuilder("Kick", nameof(LogReprimandType.Kick), "Kick the user"));
            modActions.Add(new SelectMenuOptionBuilder("Mute", nameof(LogReprimandType.Mute), "Mute the user"));
            modActions.Add(new SelectMenuOptionBuilder("Hard Mute", nameof(LogReprimandType.HardMute), "Hard Mute the user"));
        }

        components.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu($"mod-menu:{state.User.Id}", modActions, "Moderation actions...", minValues: 1, disabled: p.ShouldDisable()));

        // Navigation (outside container) - 5 components max per row
        components.WithActionRow(new ActionRowBuilder()
            .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
            .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
            .AddNextButton(p, "▶", ButtonStyle.Secondary)
            .WithButton("Refresh", "history-refresh", ButtonStyle.Primary, new Emoji("🔄"), disabled: p.ShouldDisable())
            .AddStopButton(p, "Close", ButtonStyle.Danger)
        );

        // Footer (outside container)
        components.WithTextDisplay($"-# Requested by {state.RequestedBy.Mention}");

        // Create attachment from cached bytes only on first page
        Func<IEnumerable<FileAttachment>> attachmentFactory = p.CurrentPageIndex == 0
            ? () => [new FileAttachment(new MemoryStream(state.HistoryImageBytes), "reprimand_history.png")]
            : Array.Empty<FileAttachment>;
        
        return new PageBuilder()
            .WithComponents(components.Build())
            .WithAllowedMentions(AllowedMentions.None)
            .WithAttachmentsFactory(attachmentFactory)
            .Build();
    }

    public async Task ReplyUserAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var builders = await GetUserAsync(context, user);
        var embeds = builders.Select(e => e.Build()).ToList();

        const uint defaultAccentColor = 0x9B59FF;
        var accentColor = embeds.FirstOrDefault()?.Color?.RawValue ?? defaultAccentColor;

        var container = new ContainerBuilder()
            .WithAccentColor(accentColor);

        for (var i = 0; i < embeds.Count; i++)
        {
            container.WithSection(embeds[i].ToComponentsV2Section(maxChars: 3500));

            if (i < embeds.Count - 1)
                container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
        }

        container
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay($"-# Requested by {context.User.Mention}");

        var builder = new ComponentBuilderV2()
            .WithContainer(container);

        await AddUserMenusAsync(builder, context, user);

        await context.ReplyAsync(
            components: builder.Build(),
            ephemeral: ephemeral,
            allowedMentions: AllowedMentions.None);
    }

    private static SelectMenuBuilder HistoryMenu(GuildUserEntity userEntity, ModerationCategory? category = null, LogReprimandType type = LogReprimandType.None)
    {
        category ??= userEntity.DefaultCategory ?? ModerationCategory.None;

        var types = Enum.GetValues<LogReprimandType>()[1..^1];

        var menu = new SelectMenuBuilder()
            .WithCustomId($"reprimand:{userEntity.Id}:{category.Name}")
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
            await authService.IsAuthorizedAsync(context, Scope) ||
            await authService.IsCategoryAuthorizedAsync(context, Scope);

        var userEntity = await db.Users.TrackUserAsync(user, context.Guild);
        var guildUser = user as SocketGuildUser;

        var embeds = new List<EmbedBuilder>();
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
            .WithDescription(user.Mention)
            .AddField("Created", user.CreatedAt.ToUniversalTimestamp());
        embeds.Add(embed);

        if (userEntity.JoinedAt is not null)
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
        var banDetails = userEntity.Reprimands<Ban>(null).MaxBy(b => b.Action?.Date);
        if (banDetails is not null)
            embeds.Add(banDetails.ToEmbedBuilder(true));
        else
            embed.AddField("Banned", $"This user is banned. Reason: {ban.Reason ?? "None"}");

        return embeds;
    }

    private async Task AddUserMenusAsync(ComponentBuilderV2 builder, Context context, IUser user)
    {
        var auth = await authService.IsAuthorizedAsync(context, Scope);
        var category = await authService.IsCategoryAuthorizedAsync(context, Scope);
        if (!auth && !category)
            return;

        var userEntity = await db.Users.TrackUserAsync(user, context.Guild);

        builder.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(HistoryMenu(userEntity)));

        builder.WithActionRow(new ActionRowBuilder()
            .WithSelectMenu(ReprimandMenu(user)));
    }
}