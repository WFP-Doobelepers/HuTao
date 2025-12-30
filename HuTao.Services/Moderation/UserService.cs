using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            .Where(r => r.UserId == user.Id)
            .OfType(type).OfCategory(category)
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
    private const int TargetTruncateLength = 80;

    /// <summary>
    /// Calculates the visible length of text, ignoring markdown link URLs
    /// </summary>
    private static int GetVisibleTextLength(string text)
    {
        // Match markdown links: [text](url)
        var linkPattern = @"\[([^\]]+)\]\([^\)]+\)";
        var matches = System.Text.RegularExpressions.Regex.Matches(text, linkPattern);
        
        var visibleLength = text.Length;
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            // Subtract the URL part length (everything except the [text] part)
            var fullLinkLength = match.Value.Length;
            var textPartLength = match.Groups[1].Value.Length + 2; // +2 for the []
            visibleLength -= (fullLinkLength - textPartLength);
        }
        
        return visibleLength;
    }

    /// <summary>
    /// Intelligently truncates text while preserving markdown links
    /// </summary>
    private static string TruncatePreservingLinks(string text, int maxVisibleLength)
    {
        var visibleLength = GetVisibleTextLength(text);
        if (visibleLength <= maxVisibleLength)
            return text;

        // Find all markdown links
        var linkPattern = @"\[([^\]]+)\]\([^\)]+\)";
        var matches = System.Text.RegularExpressions.Regex.Matches(text, linkPattern);
        
        // If no links, simple truncation
        if (matches.Count == 0)
        {
            return text.Substring(0, Math.Min(maxVisibleLength, text.Length)) + "...";
        }

        // Build result preserving links
        var result = new System.Text.StringBuilder();
        var currentPos = 0;
        var currentVisibleLength = 0;

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            // Add text before this link
            var beforeLink = text.Substring(currentPos, match.Index - currentPos);
            var beforeLinkVisible = beforeLink.Length;
            
            if (currentVisibleLength + beforeLinkVisible > maxVisibleLength)
            {
                // Truncate before link
                var remaining = maxVisibleLength - currentVisibleLength;
                result.Append(beforeLink.Substring(0, Math.Max(0, remaining)));
                result.Append("...");
                return result.ToString();
            }
            
            result.Append(beforeLink);
            currentVisibleLength += beforeLinkVisible;
            
            // Add the link
            var linkTextLength = match.Groups[1].Value.Length;
            if (currentVisibleLength + linkTextLength + 2 > maxVisibleLength) // +2 for []
            {
                result.Append("...");
                return result.ToString();
            }
            
            result.Append(match.Value); // Full link with URL
            currentVisibleLength += linkTextLength + 2;
            currentPos = match.Index + match.Length;
        }
        
        // Add remaining text after last link
        var remainingText = text.Substring(currentPos);
        if (currentVisibleLength + remainingText.Length > maxVisibleLength)
        {
            var space = maxVisibleLength - currentVisibleLength;
            result.Append(remainingText.Substring(0, Math.Max(0, space)));
            result.Append("...");
        }
        else
        {
            result.Append(remainingText);
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Builds reprimand content text including notes as bullet points and extracts media
    /// </summary>
    private static (string Text, List<MediaGalleryItemProperties> Media) BuildReprimandContent(
        Reprimand reprimand, IReadOnlyList<Reprimand> allReprimands)
    {
        var text = new System.Text.StringBuilder();
        var reason = reprimand.Action?.Reason ?? "No reason provided";
        
        // Main reason
        text.AppendLine($">>> {reason}");
        
        // Get attached notes
        var notes = Utilities.MediaParsingHelper.GetAttachedNotes(reprimand, allReprimands);
        if (notes.Any())
        {
            foreach (var note in notes)
            {
                var noteReason = note.Action?.Reason ?? "";
                if (!string.IsNullOrWhiteSpace(noteReason))
                {
                    text.AppendLine($"-# - {noteReason}");
                }
            }
        }
        
        // Extract images from reason and notes
        var imageUrls = Utilities.MediaParsingHelper.ExtractImageUrls(reason);
        foreach (var note in notes)
        {
            var noteReason = note.Action?.Reason ?? "";
            imageUrls.AddRange(Utilities.MediaParsingHelper.ExtractImageUrls(noteReason));
        }
        
        // Create media items with NSFW detection
        var mediaItems = imageUrls
            .Distinct()
            .Select(url => Utilities.MediaParsingHelper.CreateMediaItem(url, reason))
            .ToList();
        
        return (text.ToString().TrimEnd(), mediaItems);
    }

    /// <summary>
    /// Renders reprimands in chronological order with full details and day dividers.
    /// Collapsed (5 items): Separate containers per reprimand.
    /// Expanded (10 items): Single main container.
    /// </summary>
    private static void RenderChronologicalReprimands(ComponentBuilderV2 components, 
        IEnumerable<Reprimand> reprimands, UserHistoryPaginatorState state, IComponentPaginator p)
    {
        var reprimandList = reprimands.ToList();
        DateTimeOffset? lastDate = null;
        
        if (!state.IsChronologicalExpanded)
        {
            // Collapsed (5 items): Use separate containers to avoid component limit
            for (int i = 0; i < reprimandList.Count; i++)
            {
                var reprimand = reprimandList[i];
                var reprimandInfo = state.GetReprimandDisplayInfo(reprimand);
                var currentDate = reprimand.Action?.Date.Date;
                var isFirstReprimand = i == 0;
                var isLastReprimand = i == reprimandList.Count - 1;
                
                // Add day divider if date changed
                if (lastDate.HasValue && currentDate.HasValue && currentDate.Value.Date != lastDate.Value.Date)
                {
                    components.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
                }
                lastDate = currentDate;
                
                var container = new ContainerBuilder();
                
                // Build content with notes and images
                var (contentText, mediaItems) = BuildReprimandContent(reprimand, state.AllReprimands);
                var headerText = $"### {reprimandInfo.TypeBadges} {reprimandInfo.StatusBadge} • [{reprimandInfo.ShortId}]\n" +
                                $"-# {reprimandInfo.Moderator} {reprimandInfo.DateShort} {reprimandInfo.TimeShort} • {reprimandInfo.RelativeTime}\n" +
                                contentText;
                
                if (isFirstReprimand)
                {
                    var section = new SectionBuilder()
                        .WithTextDisplay(headerText)
                        .WithAccessory(new ButtonBuilder("☰", $"history-group:{state.User.Id}", 
                            ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                    container.WithSection(section);
                }
                else if (isLastReprimand)
                {
                    var section = new SectionBuilder()
                        .WithTextDisplay(headerText)
                        .WithAccessory(new ButtonBuilder("▼", $"history-expand:{state.User.Id}", 
                            ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                    container.WithSection(section);
                }
                else
                {
                    container.WithTextDisplay(headerText);
                }
                
                // Add media gallery if there are images
                if (mediaItems.Any())
                {
                    container.WithMediaGallery(mediaItems);
                }
                
                components.WithContainer(container);
            }
        }
        else
        {
            // Expanded (10 items): Single main container
            var mainContainer = new ContainerBuilder();
            
            for (int i = 0; i < reprimandList.Count; i++)
            {
                var reprimand = reprimandList[i];
                var reprimandInfo = state.GetReprimandDisplayInfo(reprimand);
                var currentDate = reprimand.Action?.Date.Date;
                var isFirstReprimand = i == 0;
                var isLastReprimand = i == reprimandList.Count - 1;
                
                // Add day divider if date changed
                if (lastDate.HasValue && currentDate.HasValue && currentDate.Value.Date != lastDate.Value.Date)
                {
                    mainContainer.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
                }
                lastDate = currentDate;
                
                // Build content with notes and images
                var (contentText, mediaItems) = BuildReprimandContent(reprimand, state.AllReprimands);
                var headerText = $"### {reprimandInfo.TypeBadges} {reprimandInfo.StatusBadge} • [{reprimandInfo.ShortId}]\n" +
                                $"-# {reprimandInfo.Moderator} {reprimandInfo.DateShort} {reprimandInfo.TimeShort} • {reprimandInfo.RelativeTime}\n" +
                                contentText;
                
                if (isFirstReprimand)
                {
                    var section = new SectionBuilder()
                        .WithTextDisplay(headerText)
                        .WithAccessory(new ButtonBuilder("☰", $"history-group:{state.User.Id}", 
                            ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                    mainContainer.WithSection(section);
                }
                else if (isLastReprimand)
                {
                    var section = new SectionBuilder()
                        .WithTextDisplay(headerText)
                        .WithAccessory(new ButtonBuilder("▲", $"history-expand:{state.User.Id}", 
                            ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                    mainContainer.WithSection(section);
                }
                else
                {
                    mainContainer.WithTextDisplay(headerText);
                }
                
                // Add media gallery if there are images
                if (mediaItems.Any())
                {
                    mainContainer.WithMediaGallery(mediaItems);
                }
            }
            
            components.WithContainer(mainContainer);
        }
    }

    /// <summary>
    /// Renders reprimands grouped by type in condensed format.
    /// Collapsed (truncated): Separate containers per group.
    /// Expanded (full reasons): Single main container.
    /// </summary>
    private static void RenderGroupedReprimands(ComponentBuilderV2 components, 
        IEnumerable<Reprimand> reprimands, UserHistoryPaginatorState state, IComponentPaginator p)
    {
        var reprimandList = reprimands.ToList();
        var grouped = reprimandList
            .GroupBy(r => r.GetTitle(showId: false))
            .OrderBy(g => g.Key)
            .ToList();
        
        if (!state.IsGroupedExpanded)
        {
            // Collapsed (truncated): Use separate containers per group
            // IMPORTANT: Track TOTAL message text (all components combined) - Discord limit is 4000 chars for entire message
            const int maxTotalMessageText = 3500; // Conservative limit leaving room for header, footer, buttons
            bool isFirstGroup = true;
            var totalMessageTextLength = 0;
            var currentPageReprimands = reprimandList.Skip(p.CurrentPageIndex * state.GetReprimandsPerPage()).Take(state.GetReprimandsPerPage()).ToList();
            
            // Re-group the current page's reprimands
            var pageGrouped = currentPageReprimands
                .GroupBy(r => r.GetTitle(showId: false))
                .OrderBy(g => g.Key)
                .ToList();
            
            foreach (var group in pageGrouped)
            {
                var entries = group.OrderByDescending(r => r.Action?.Date).ToList();
                var container = new ContainerBuilder();
                
                // Get badges for this reprimand type (use first entry since all have same type)
                var firstEntry = entries.First();
                var typeBadges = Utilities.ReprimandBadgeHelper.GetTypeOnly(firstEntry);
                
                // Build condensed entries for this type using bullet points
                var condensedText = new System.Text.StringBuilder();
                condensedText.AppendLine($"### {typeBadges}");
                var headerLength = condensedText.Length;
                
                // Check if adding this group header would exceed TOTAL MESSAGE limit
                if (totalMessageTextLength + headerLength > maxTotalMessageText)
                    break;
                
                foreach (var reprimand in entries)
                {
                    var reason = reprimand.Action?.Reason ?? "No reason provided";
                    var date = reprimand.Action?.Date ?? DateTimeOffset.UtcNow;
                    var dateStr = $"<t:{date.ToUnixTimeSeconds()}:d>";
                    var displayReason = TruncatePreservingLinks(reason, TargetTruncateLength);
                    var entry = $"• {displayReason} • {dateStr}\n";
                    
                    // Check if adding this entry would exceed TOTAL MESSAGE limit
                    if (totalMessageTextLength + condensedText.Length + entry.Length > maxTotalMessageText)
                        break;
                    
                    condensedText.Append(entry);
                }
                
                // Skip empty groups
                if (condensedText.Length <= headerLength)
                    continue;
                
                // First group gets the chronological toggle button
                if (isFirstGroup)
                {
                    var section = new SectionBuilder()
                        .WithTextDisplay(condensedText.ToString().TrimEnd())
                        .WithAccessory(new ButtonBuilder("📅", $"history-group:{state.User.Id}", 
                            ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                    container.WithSection(section);
                    isFirstGroup = false;
                }
                else
                {
                    container.WithTextDisplay(condensedText.ToString().TrimEnd());
                }
                
                components.WithContainer(container);
                totalMessageTextLength += condensedText.Length;
            }
            
            // Add expand button at the end
            var expandContainer = new ContainerBuilder();
            var expandSection = new SectionBuilder()
                .WithTextDisplay("-# Showing shortened reasons")
                .WithAccessory(new ButtonBuilder("▼", $"history-expand:{state.User.Id}", 
                    ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
            expandContainer.WithSection(expandSection);
            components.WithContainer(expandContainer);
        }
        else
        {
            // Expanded (full reasons): Single container with multiple TextDisplays
            // IMPORTANT: Track TOTAL message text (all components combined) - Discord limit is 4000 chars for entire message
            const int maxTotalMessageText = 3500; // Conservative limit leaving room for header, footer, buttons
            var mainContainer = new ContainerBuilder();
            bool isFirstGroup = true;
            var totalMessageTextLength = 0;
            var processedCount = 0;
            var currentPageReprimands = reprimandList.Skip(p.CurrentPageIndex * state.GetReprimandsPerPage()).Take(state.GetReprimandsPerPage()).ToList();
            
            // Re-group the current page's reprimands
            var pageGrouped = currentPageReprimands
                .GroupBy(r => r.GetTitle(showId: false))
                .OrderBy(g => g.Key)
                .ToList();
            
            foreach (var group in pageGrouped)
            {
                var entries = group.OrderByDescending(r => r.Action?.Date).ToList();
                
                // Get badges for this reprimand type (use first entry since all have same type)
                var firstEntry = entries.First();
                var typeBadges = Utilities.ReprimandBadgeHelper.GetTypeOnly(firstEntry);
                
                // Build group text
                var groupText = new System.Text.StringBuilder();
                groupText.AppendLine($"### {typeBadges}");
                var headerLength = groupText.Length;
                
                // Check if adding this group header would exceed TOTAL MESSAGE limit
                if (totalMessageTextLength + headerLength > maxTotalMessageText && processedCount > 0)
                    break;
                
                foreach (var reprimand in entries)
                {
                    var reason = reprimand.Action?.Reason ?? "No reason provided";
                    var date = reprimand.Action?.Date ?? DateTimeOffset.UtcNow;
                    var dateStr = $"<t:{date.ToUnixTimeSeconds()}:d>";
                    var entry = $"• {reason} • {dateStr}\n";
                    
                    // Check if adding this entry would exceed TOTAL MESSAGE limit
                    if (totalMessageTextLength + groupText.Length + entry.Length > maxTotalMessageText)
                        break; // Stop adding entries, move to next page
                    
                    groupText.Append(entry);
                    processedCount++;
                }
                
                // Output this group
                if (groupText.Length > headerLength)
                {
                    if (isFirstGroup)
                    {
                        var section = new SectionBuilder()
                            .WithTextDisplay(groupText.ToString().TrimEnd())
                            .WithAccessory(new ButtonBuilder("📅", $"history-group:{state.User.Id}", 
                                ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
                        mainContainer.WithSection(section);
                        isFirstGroup = false;
                    }
                    else
                    {
                        mainContainer.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
                        mainContainer.WithTextDisplay(groupText.ToString().TrimEnd());
                    }
                    
                    totalMessageTextLength += groupText.Length;
                }
            }
            
            // Add collapse button at the end
            mainContainer.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
            var expandSection = new SectionBuilder()
                .WithTextDisplay("-# Showing full reasons")
                .WithAccessory(new ButtonBuilder("▲", $"history-expand:{state.User.Id}", 
                    ButtonStyle.Secondary, isDisabled: p.ShouldDisable()));
            mainContainer.WithSection(expandSection);
            
            components.WithContainer(mainContainer);
        }
    }

    /// <summary>
    /// Page factory method for Components V2 user history paginator
    /// </summary>
    private static IPage GenerateUserHistoryPage(IComponentPaginator p, UserHistoryPaginatorState state)
    {
        var currentReprimands = state.GetReprimandsForPage(p.CurrentPageIndex).ToList();
        var components = new ComponentBuilderV2();
        
        // Get user avatar URL
        var avatarUrl = state.User.GetDisplayAvatarUrl(size: 256) ?? state.User.GetDefaultAvatarUrl();
        
        // First page: Full history image with created/joined timestamps
        // Header without thumbnail accessory to prevent layout issues
        var createdTimestamp = $"<t:{state.User.CreatedAt.ToUnixTimeSeconds()}:R> <t:{state.User.CreatedAt.ToUnixTimeSeconds()}:f>";
        var joinedTimestamp = state.UserEntity.JoinedAt != null 
            ? $"<t:{state.UserEntity.JoinedAt.Value.ToUnixTimeSeconds()}:R> <t:{state.UserEntity.JoinedAt.Value.ToUnixTimeSeconds()}:f>"
            : "Unknown";
        
        if (p.CurrentPageIndex == 0)
        {
            // Page 1: Header with basic info, history image only in expanded mode
            components.WithTextDisplay($"# {state.User.Mention}'s History\n\n" +
                                      $"-# Created {createdTimestamp}\n" +
                                      $"-# Joined   {joinedTimestamp}");
            
            // Show history image only in expanded mode
            if (state.IsChronologicalExpanded || state.IsGroupedExpanded)
            {
                var historyImageContainer = new ContainerBuilder()
                    .WithMediaGallery([new MediaGalleryItemProperties(
                        new UnfurledMediaItemProperties("attachment://reprimand_history.png"))]);
                
                components.WithContainer(historyImageContainer);
            }
            
            components.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
        }
        else
        {
            // Subsequent pages: Summary only, no image
            components.WithTextDisplay($"# {state.User.Mention}'s History\n\n" +
                                      $"-# Total Records: {state.TotalReprimands} • Filter: {state.TypeFilter.Humanize()}");
            components.WithSeparator(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Small));
        }

        // Display reprimands or empty message
        if (!currentReprimands.Any())
        {
            components.WithTextDisplay("*No reprimands found matching your criteria.*\n\n" +
                                      "This user has a clean record with the current filters applied.");
        }
        else if (state.IsGrouped)
        {
            // Grouped view: Show condensed format grouped by type
            RenderGroupedReprimands(components, currentReprimands, state, p);
        }
        else
        {
            // Chronological view: Show full format with day dividers
            RenderChronologicalReprimands(components, currentReprimands, state, p);
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
            ? () => new FileAttachment[] { new FileAttachment(new MemoryStream(state.HistoryImageBytes), "reprimand_history.png") }
            : () => Array.Empty<FileAttachment>();
        
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