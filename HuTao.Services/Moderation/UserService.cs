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
                media.Add(new MediaGalleryItemProperties(new UnfurledMediaItemProperties(guildAvatar), "Server avatar"));
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
        IUserMessage? loadingMessage = null;
        if (context is CommandContext cmd)
        {
            var loading = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("Loading history..."))
                .Build();

            loadingMessage = await cmd.Channel.SendMessageAsync(
                components: loading,
                messageReference: new MessageReference(cmd.Message.Id));
        }
        else
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

        var state = new UserHistoryPaginatorState(user, userEntity, history, category, type, guild, context.User)
        {
            IsBanned = await context.Guild.GetBanAsync(user) is not null,
            TimedOutUntil = (user as IGuildUser)?.TimedOutUntil
        };
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
            CommandContext when loadingMessage is not null
                => interactive.SendPaginatorAsync(
                    paginator, loadingMessage,
                    timeout: TimeSpan.FromMinutes(15),
                    resetTimeoutOnInput: true),

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
    private static ContainerBuilder? RenderGroupedReprimands(
        IEnumerable<Reprimand> pageReprimands, IEnumerable<Reprimand> allReprimands,
        int usedTextLength, int reservedLength = 0, bool showImages = true,
        int componentBudget = 20)
    {
        const int maxCumulativeText = 4000;
        var reprimandList = pageReprimands.ToList();
        var cumulativeTextLength = usedTextLength;
        var footerLength = reservedLength;
        var componentsUsed = 0;
        
        var attachedNotes = new Dictionary<Guid, List<Note>>();
        var claimedNotes = new HashSet<Guid>();

        var nonNotes = reprimandList
            .Where(r => r is not Note)
            .Where(r => r.Action?.Date is not null && r.Action?.Moderator is not null)
            .ToList();

        foreach (var note in reprimandList.OfType<Note>())
        {
            var noteDate = note.Action?.Date;
            var noteMod = note.Action?.Moderator?.Id;
            if (noteDate is null || noteMod is null) continue;

            var parent = nonNotes
                .Where(r => r.Action!.Moderator.Id == noteMod)
                .Where(r => Math.Abs((noteDate.Value - r.Action!.Date).TotalMinutes) <= 10)
                .MinBy(r => Math.Abs((noteDate.Value - r.Action!.Date).TotalMinutes));

            if (parent is null) continue;

            if (!attachedNotes.ContainsKey(parent.Id))
                attachedNotes[parent.Id] = [];
            attachedNotes[parent.Id].Add(note);
            claimedNotes.Add(note.Id);
        }

        var allByType = allReprimands
            .GroupBy(r => r.GetTitle(showId: false))
            .ToDictionary(g => g.Key, g => (Total: g.Count(), Inactive: g.Count(r => !IsActive(r))));
        
        var grouped = reprimandList
            .Where(r => !claimedNotes.Contains(r.Id))
            .GroupBy(r => r.GetTitle(showId: false))
            .OrderByDescending(g => g.Max(r => r.Action?.Date))
            .ToList();
        
        var container = new ContainerBuilder();
        var hasContent = false;
        var pendingText = new StringBuilder();
        var budgetExhausted = false;

        void FlushText()
        {
            if (pendingText.Length <= 0) return;
            container.WithTextDisplay(pendingText.ToString().Truncate(MaxTextDisplayLength));
            pendingText.Clear();
        }

        bool AppendText(string text)
        {
            if (pendingText.Length == 0)
            {
                if (componentsUsed >= componentBudget) return false;
                componentsUsed++;
            }
            else
                pendingText.AppendLine();
            pendingText.Append(text);
            return true;
        }
        
        var isFirstGroup = true;
        foreach (var group in grouped)
        {
            if (budgetExhausted) break;

            var entries = group
                .OrderByDescending(r => IsActive(r))
                .ThenByDescending(r => r.Action?.Date)
                .ToList();
            
            var firstEntry = entries.First();
            var typeName = firstEntry.GetTitle(showId: false);
            var (totalCount, inactiveCount) = allByType.GetValueOrDefault(typeName, (entries.Count, 0));
            var showingCount = entries.Count;
            
            var headerText = new StringBuilder();
            headerText.Append($"### {typeName.ToQuantity(totalCount)}");
            
            var subtitleParts = new List<string>();
            if (showingCount < totalCount)
                subtitleParts.Add($"Showing {showingCount}/{totalCount}");
            if (inactiveCount > 0)
                subtitleParts.Add($"{inactiveCount} inactive");
            if (subtitleParts.Count > 0)
                headerText.Append($"\n-# {string.Join(" • ", subtitleParts)}");
            
            if (cumulativeTextLength + headerText.Length + footerLength >= maxCumulativeText)
                break;

            if (!isFirstGroup)
            {
                if (componentsUsed + 2 > componentBudget) break;
                FlushText();
                container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
                componentsUsed++;
            }
            isFirstGroup = false;
            
            if (!AppendText(headerText.ToString())) break;
            cumulativeTextLength += headerText.Length;
            hasContent = true;
            
            var collapsedList = CollapseIdenticalReasons(entries).ToList();
            string? lastModerator = null;
            
            for (var i = 0; i < collapsedList.Count; i++)
            {
                var (reprimandOrGroup, count, isActive) = collapsedList[i];
                var reason = reprimandOrGroup.Action?.Reason ?? "No reason provided";
                var date = reprimandOrGroup.Action?.Date ?? DateTimeOffset.UtcNow;
                var moderator = reprimandOrGroup.Action?.Moderator is { } mod ? $"<@{mod.Id}>" : "System";
                var relativeStr = $"<t:{date.ToUnixTimeSeconds()}:R>";
                var entryBuilder = new StringBuilder();
                
                if (count > 1)
                {
                    var nextMod = i + 1 < collapsedList.Count
                        ? collapsedList[i + 1].Reprimand.Action?.Moderator is { } nm ? $"<@{nm.Id}>" : "System"
                        : null;
                    var modHasMoreEntries = moderator == lastModerator || moderator == nextMod;
                    
                    if (moderator != lastModerator)
                    {
                        entryBuilder.AppendLine(modHasMoreEntries
                            ? $"-# {moderator}"
                            : $"-# {moderator} • {relativeStr}");
                        lastModerator = moderator;
                    }
                    
                    var mergedContent = modHasMoreEntries
                        ? $"**x{count}** {reason} • {relativeStr}"
                        : $"**x{count}** {reason}";
                    entryBuilder.AppendLine(FormatQuotedContent(mergedContent, isActive));
                }
                else
                {
                    if (moderator != lastModerator)
                    {
                        entryBuilder.AppendLine($"-# {moderator} • {relativeStr}");
                        lastModerator = moderator;
                    }
                    else
                    {
                        entryBuilder.AppendLine($"-# {relativeStr}");
                    }
                    entryBuilder.AppendLine(FormatQuotedContent(reason, isActive));
                }

                if (attachedNotes.TryGetValue(reprimandOrGroup.Id, out var notes))
                {
                    entryBuilder.AppendLine("-# Notes");
                    foreach (var note in notes)
                    {
                        var noteContent = note.Action?.Reason ?? "No content";
                        foreach (var line in noteContent.Split('\n'))
                            entryBuilder.AppendLine($"-# - {line}");
                    }
                }

                if (cumulativeTextLength + entryBuilder.Length + footerLength >= maxCumulativeText)
                    break;

                var entryStr = entryBuilder.ToString().TrimEnd();
                cumulativeTextLength += entryStr.Length;

                var entryMedia = new List<MediaGalleryItemProperties>();
                (string Url, string Label)? messageLink = null;

                if (attachedNotes.TryGetValue(reprimandOrGroup.Id, out var noteMedia))
                {
                    foreach (var note in noteMedia)
                    {
                        var noteReason = note.Action?.Reason ?? "";
                        entryMedia.AddRange(MediaParsingHelper.ExtractAndCreateMediaItems(noteReason));
                        messageLink ??= MediaParsingHelper.ExtractFirstMessageLink(noteReason);

                        if (messageLink is { Label: var noteLabel } && MediaParsingHelper.IsLikelyImageUrl(noteLabel))
                            entryMedia.Insert(0, MediaParsingHelper.CreateMediaItem(noteLabel, noteReason));
                    }
                }

                entryMedia.AddRange(MediaParsingHelper.ExtractAndCreateMediaItems(reason));
                messageLink ??= MediaParsingHelper.ExtractFirstMessageLink(reason);

                if (messageLink is { Label: var label } && MediaParsingHelper.IsLikelyImageUrl(label))
                    entryMedia.Insert(0, MediaParsingHelper.CreateMediaItem(label, reason));

                const int sectionCost = 3;
                const int galleryCost = 1;

                if (entryMedia.Count > 0)
                {
                    if (showImages)
                    {
                        if (messageLink is { } link && componentsUsed + sectionCost + galleryCost <= componentBudget)
                        {
                            FlushText();
                            container.WithSection(new SectionBuilder()
                                .WithTextDisplay(entryStr)
                                .WithAccessory(ButtonBuilder.CreateLinkButton(link.Label, link.Url)));
                            componentsUsed += sectionCost;
                            container.WithMediaGallery(entryMedia);
                            componentsUsed += galleryCost;
                        }
                        else if (componentsUsed + 1 + galleryCost <= componentBudget)
                        {
                            FlushText();
                            container.WithTextDisplay(entryStr);
                            componentsUsed++;
                            container.WithMediaGallery(entryMedia);
                            componentsUsed += galleryCost;
                        }
                        else if (!AppendText(entryStr))
                        {
                            budgetExhausted = true; break;
                        }
                    }
                    else if (componentsUsed + sectionCost <= componentBudget)
                    {
                        FlushText();
                        container.WithSection(new SectionBuilder()
                            .WithTextDisplay(entryStr)
                            .WithAccessory(new ThumbnailBuilder(entryMedia[0].Media, isSpoiler: entryMedia[0].IsSpoiler)));
                        componentsUsed += sectionCost;
                    }
                    else if (!AppendText(entryStr))
                    {
                        budgetExhausted = true; break;
                    }
                }
                else if (messageLink is { } link && componentsUsed + sectionCost <= componentBudget)
                {
                    FlushText();
                    container.WithSection(new SectionBuilder()
                        .WithTextDisplay(entryStr)
                        .WithAccessory(ButtonBuilder.CreateLinkButton(link.Label, link.Url)));
                    componentsUsed += sectionCost;
                }
                else if (!AppendText(entryStr))
                {
                    budgetExhausted = true; break;
                }
            }
        }
        
        FlushText();
        
        return hasContent ? container : null;
    }
    
    private static string FormatQuotedContent(string content, bool isActive)
    {
        if (isActive)
            return "> " + content.Replace("\n", "\n> ");
        
        var lines = content.Split('\n');
        var result = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) result.Append('\n');
            result.Append($"> -# ~~{lines[i]}~~");
        }
        result.Append(" • [inactive]");
        return result.ToString();
    }
    
    private static bool IsActive(Reprimand r)
        => r is ExpirableReprimand e ? e.IsActive() : r.Status is ReprimandStatus.Added;

    /// <summary>
    /// Collapses reprimands with identical reasons and active status into groups with counts.
    /// </summary>
    private static IEnumerable<(Reprimand Reprimand, int Count, bool IsActive)> CollapseIdenticalReasons(IEnumerable<Reprimand> reprimands)
        => reprimands
            .GroupBy(r => (Reason: r.Action?.Reason ?? "", IsActive: IsActive(r)))
            .Select(g => (Reprimand: g.First(), Count: g.Count(), g.Key.IsActive));

    /// <summary>
    /// Page factory method for Components V2 user history paginator
    /// </summary>
    private static IPage GenerateUserHistoryPage(IComponentPaginator p, UserHistoryPaginatorState state)
    {
        var currentReprimands = state.GetReprimandsForPage(p.CurrentPageIndex).ToList();
        var components = new ComponentBuilderV2();
        
        var createdTimestamp = $"<t:{state.User.CreatedAt.ToUnixTimeSeconds()}:R>";
        var joinedTimestamp = state.UserEntity.JoinedAt != null 
            ? $"<t:{state.UserEntity.JoinedAt.Value.ToUnixTimeSeconds()}:R>"
            : "Unknown";
        
        var headerBuilder = new StringBuilder();
        headerBuilder.Append($"### {state.User.Mention}'s History\n");
        headerBuilder.Append(p.CurrentPageIndex == 0
            ? $"-# Created {createdTimestamp} • Joined {joinedTimestamp}"
            : $"-# Page {p.CurrentPageIndex + 1} • {state.TotalReprimands} records");
        
        var indicators = new List<string>();
        
        if (state.IsBanned)
        {
            var banReprimand = state.AllReprimands.OfType<Ban>().FirstOrDefault(r => r.IsActive());
            var banDate = banReprimand?.Action?.Date ?? banReprimand?.StartedAt;
            indicators.Add(banDate is not null
                ? $"Banned <t:{banDate.Value.ToUnixTimeSeconds()}:R>"
                : "Banned");
        }
        
        if (state.TimedOutUntil is { } apiTimeout && apiTimeout > DateTimeOffset.UtcNow)
        {
            indicators.Add($"Timed out <t:{apiTimeout.ToUnixTimeSeconds()}:R>");
        }
        else
        {
            var timeoutReprimand = state.AllReprimands.OfType<Timeout>().FirstOrDefault(r => r.IsActive());
            if (timeoutReprimand is not null)
            {
                var ts = timeoutReprimand.ExpireAt ?? timeoutReprimand.StartedAt;
                indicators.Add($"Timed out <t:{ts.ToUnixTimeSeconds()}:R>");
            }
        }
        
        var activeMute = state.AllReprimands.OfType<Mute>().Where(r => r is not HardMute).FirstOrDefault(r => r.IsActive());
        if (activeMute is not null)
            indicators.Add($"Muted <t:{(activeMute.Action?.Date ?? activeMute.StartedAt).ToUnixTimeSeconds()}:R>");
        
        var activeHardMute = state.AllReprimands.OfType<HardMute>().FirstOrDefault(r => r.IsActive());
        if (activeHardMute is not null)
            indicators.Add($"Hard Muted <t:{(activeHardMute.Action?.Date ?? activeHardMute.StartedAt).ToUnixTimeSeconds()}:R>");
        
        if (indicators.Count > 0)
            headerBuilder.Append($"\n-# {string.Join(" • ", indicators)}");
        
        var headerText = headerBuilder.ToString();
        
        var avatarUrl = state.User.GetDisplayAvatarUrl(size: 256)
            ?? state.User.GetDefaultAvatarUrl();
        var headerContainer = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder(headerText)],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(avatarUrl)));
        components.WithContainer(headerContainer);

        if (!currentReprimands.Any())
        {
            components.WithContainer(new ContainerBuilder()
                .WithTextDisplay("*No reprimands found matching your criteria.*\n\n" +
                                 "This user has a clean record with the current filters applied."));
        }
        else
        {
            var footerText = $"-# Requested by {state.RequestedBy.Mention}";
            var hasImages = currentReprimands.Any(r =>
                MediaParsingHelper.ExtractImageUrls(r.Action?.Reason ?? "").Count > 0);
            var fixedOverhead = 4  // header container (container + section + text + thumbnail)
                + 1                // reprimand container itself
                + (hasImages ? 3 : 1) // footer section or text display
                + 2                // mod menu row (actionrow + selectmenu)
                + 4                // nav row (actionrow + 3 buttons)
                + (state.Guild.ModerationCategories.Any() ? 2 : 0);
            var contentBudget = ComponentsV2Validator.MaxTotalComponents - fixedOverhead;
            var reprimandContainer = RenderGroupedReprimands(
                currentReprimands, state.FilteredReprimands,
                headerText.Length, footerText.Length, state.ShowImages,
                contentBudget);

            if (reprimandContainer is not null)
            {
                if (hasImages)
                {
                    reprimandContainer.WithSection(new SectionBuilder()
                        .WithTextDisplay(footerText)
                        .WithAccessory(new ButtonBuilder(
                            customId: "history-toggle-images",
                            style: ButtonStyle.Secondary,
                            emote: new Emoji("🛂"),
                            isDisabled: p.ShouldDisable())));
                }
                else
                {
                    reprimandContainer.WithTextDisplay(footerText);
                }

                components.WithContainer(reprimandContainer);
            }
        }

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
        );

        var builtComponents = components.Build();

        ComponentsV2Validator.AssertValid(builtComponents, $"UserHistory page {p.CurrentPageIndex}");

        return new PageBuilder()
            .WithComponents(builtComponents)
            .WithAllowedMentions(AllowedMentions.None)
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

        container.WithTextDisplay($"-# Requested by {context.User.Mention}");

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