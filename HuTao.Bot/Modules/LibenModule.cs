using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Evaluation;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using static HuTao.Bot.Modules.Box;
using static HuTao.Bot.Modules.Box.Element;
using static HuTao.Services.TimeTracking.GenshinTimeTrackingService;

namespace HuTao.Bot.Modules;

public class GenshinRegisterModule(IMemoryCache cache, InteractionService commands) : ModuleBase<SocketCommandContext>
{
    [Command("liben register")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task RegisterCommandsAsync(ITextChannel? channel = null)
    {
        var modules = commands.Modules.Where(m => m.DontAutoRegister).ToArray();
        await commands.AddModulesToGuildAsync(Context.Guild, false, modules);
        cache.Set($"{nameof(LibenModule)}.Channel.{Context.Guild.Id}", channel ?? Context.Channel as ITextChannel);

        await ReplyAsync("Registered commands");
    }
}

[DontAutoRegister]
[Discord.Interactions.Group("liben", "Marvelous Merchandise Commands")]
public class LibenModule(
    DiscordSocketClient client,
    HttpClient http,
    IMemoryCache cache,
    InteractiveService interactive)
    : InteractionModuleBase<SocketInteractionContext>,
      INotificationHandler<MessageReceivedNotification>
{
    private const string EventImage = "https://pbs.twimg.com/media/FR1JQa-aUAA_rAo.png";
    private const string LibenImage = "https://static.wikia.nocookie.net/gensin-impact/images/1/10/NPC_Liben.png";

    private static readonly Dictionary<Element, Emote> BoxEmotes = new()
    {
        [Anemo]   = Emote.Parse("<:Anemo:925598874779910215>"),
        [Cryo]    = Emote.Parse("<:Cryo:925598873731334175>"),
        [Dendro]  = Emote.Parse("<:Dendro:925598874532479076>"),
        [Electro] = Emote.Parse("<:Electro:925598875174199327>"),
        [Geo]     = Emote.Parse("<:Geo:925598873983017000>"),
        [Hydro]   = Emote.Parse("<:Hydro:925598873844604958>"),
        [Pyro]    = Emote.Parse("<:Pyro:925598875853668353>")
    };

    private static readonly Dictionary<ulong, ServerRegion> RegionRoles = new()
    {
        [920402612040380477] = ServerRegion.America,
        [793700708959780884] = ServerRegion.Europe,
        [793700837959663636] = ServerRegion.Asia,
        [823711464933163018] = ServerRegion.SAR
    };

    private static readonly EmbedBuilder LibenEmbed = Liben()
        .WithColor(0x422a54)
        .WithImageUrl(EventImage)
        .WithFooter("The buttons below show [The boxes in list | People looking for this]")
        .WithDescription(new StringBuilder()
            .AppendLine(
                "Hey guys, glad to see you here! To keep things streamlined I've made an interactive way to find a box that you need..")
            .AppendLine()
            .AppendLine(
                "Click on the **Blue** buttons to show the list, you might want to press on the Cryo or Pyro buttons it's real popular!")
            .AppendLine(
                "You may click on the **Green** buttons if you want to add your box, or queue up if there is none.")
            .AppendLine()
            .AppendLine($"You can also type out {Format.Bold(Format.Code("/liben"))} to get started.").ToString());

    private ulong? _guildId;

    protected SemaphoreSlim ExpiredSemaphore => cache.GetOrCreate(ExpiredSemaphoreKey, _ => new SemaphoreSlim(1, 1))!;

    protected SemaphoreSlim Semaphore => cache.GetOrCreate(SemaphoreKey, _ => new SemaphoreSlim(1, 1))!;

    private bool IsBottom
    {
        get => cache.GetOrCreate(IsBottomKey, _ => false);
        set => cache.Set(IsBottomKey, value);
    }

    private CancellationTokenSource TokenSource
    {
        get => cache.GetOrCreate(TokenSourceKey, _ => new CancellationTokenSource())!;
        set => cache.Set(TokenSourceKey, value);
    }

    private ConcurrentDictionary<(ulong Id, Element Element), Looking> LookingUsers
        => cache.GetOrCreate(LookingKey, _ => new ConcurrentDictionary<(ulong, Element), Looking>())!;

    private ConcurrentDictionary<ulong, Box> CachedBoxes
        => cache.GetOrCreate(BoxKey, _ => new ConcurrentDictionary<ulong, Box>())!;

    private IMessageChannel? Channel => cache.Get<IMessageChannel?>(ChannelKey);

    private IUserMessage? LastMessage
    {
        get => cache.Get<IUserMessage?>(LastMessageKey);
        set => cache.Set(LastMessageKey, value);
    }

    private string BoxKey => $"{nameof(LibenModule)}.{nameof(CachedBoxes)}.{GuildId}";

    private string ChannelKey => $"{nameof(LibenModule)}.{nameof(Channel)}.{GuildId}";

    private string ExpiredSemaphoreKey => $"{nameof(LibenModule)}.{nameof(ExpiredSemaphore)}.{GuildId}";

    private string IsBottomKey => $"{nameof(LibenModule)}.{nameof(IsBottom)}.{GuildId}";

    private string LastMessageKey => $"{nameof(LibenModule)}.{nameof(LastMessage)}.{GuildId}";

    private string LookingKey => $"{nameof(LibenModule)}.{nameof(LookingUsers)}.{GuildId}";

    private string SemaphoreKey => $"{nameof(LibenModule)}.{nameof(Semaphore)}.{GuildId}";

    private string TokenSourceKey => $"{nameof(LibenModule)}.{nameof(TokenSource)}.{GuildId}";

    private ulong GuildId => _guildId ?? Context.Guild.Id;

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is not ITextChannel channel) return;
        _guildId = channel.Guild.Id;

        if (notification.Message.Channel.Id == Channel?.Id
            && !notification.Message.Author.IsBot
            && !notification.Message.Author.IsWebhook)
        {
            IsBottom = false;
            await RefreshAsync();
        }
    }

    [SlashCommand("add", "Add yourself to the list of marvelous merchandise.")]
    public async Task AddAsync(
        Element element, ServerRegion region,
        [Discord.Interactions.Summary(description: "Your world level. Must be 1-8.")]
        uint worldLevel,
        [Discord.Interactions.Summary(description: "Your UID in the game.")]
        ulong id,
        [Discord.Interactions.Summary(description: "Delete your box after this time. Default 30 minutes.")]
        TimeSpan? deleteAfter = null)
    {
        if (deleteAfter <= TimeSpan.Zero)
        {
            await RespondAsync("Invalid Delete Length", ephemeral: true);
            return;
        }

        if (worldLevel is > 8 or < 1)
        {
            await RespondAsync("Invalid World Level", ephemeral: true);
            return;
        }

        var box = new Box(Context.User, id, worldLevel, element, region, deleteAfter);
        CachedBoxes[Context.User.Id] = box;

        await DeferAsync(true);
        var components = BuildBoxComponents(box, "✅ Added your box");
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Embeds = Array.Empty<Embed>();
        });

        await RefreshAsync();
        await ReplyFoundAsync(box);
    }

    [ComponentInteraction("add-interactive:*", true)]
    public async Task AddInteractiveAsync(bool restart)
    {
        const string message = "Hello, I will guide you through adding your box. What element are you adding?";

        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\n{message}")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var buttons = BoxEmotes.Select(kv =>
            new ButtonBuilder(kv.Key.ToString(), $"add0:{kv.Key}", ButtonStyle.Secondary, emote: kv.Value)).ToList();

        var builder = new ComponentBuilderV2().WithContainer(container);
        builder.WithActionRow(new ActionRowBuilder().WithComponents(buttons.Take(4)));
        builder.WithActionRow(new ActionRowBuilder().WithComponents(buttons.Skip(4)));

        var components = builder.Build();
        if (restart)
        {
            await DeferAsync(true);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Components = components;
                m.Embeds = Array.Empty<Embed>();
            });
        }
        else
        {
            await RespondAsync(
                ephemeral: true,
                components: components);
        }
    }

    [ComponentInteraction("add0:*", true)]
    public async Task AddInteractiveAsync(Element element)
    {
        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\nSo the element is **{element}**. What is your region?")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var row = new ActionRowBuilder();
        foreach (var region in Enum.GetValues<ServerRegion>())
            row.WithButton(new ButtonBuilder(region.ToString(), $"add1:{element},{region}", ButtonStyle.Secondary));

        row.WithButton(new ButtonBuilder("Restart", "add-interactive:true", ButtonStyle.Danger));

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(row)
            .Build();

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Embeds = Array.Empty<Embed>();
        });
    }

    [ComponentInteraction("add1:*,*", true)]
    public async Task AddInteractiveAsync(Element element, ServerRegion region)
    {
        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\nSo you have **{element}** in **{region}**. What is your world level?")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var first = new ActionRowBuilder();
        for (var i = 1; i <= 5; i++)
            first.WithButton(new ButtonBuilder($"{i}", $"add2:{element},{region},{i}", ButtonStyle.Secondary));

        var second = new ActionRowBuilder();
        for (var i = 6; i <= 8; i++)
            second.WithButton(new ButtonBuilder($"{i}", $"add2:{element},{region},{i}", ButtonStyle.Secondary));
        second.WithButton(new ButtonBuilder("Restart", "add-interactive:true", ButtonStyle.Danger));

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(first)
            .WithActionRow(second)
            .Build();

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Embeds = Array.Empty<Embed>();
        });
    }

    [ComponentInteraction("add2:*,*,*", true)]
    public Task AddInteractiveAsync(Element element, ServerRegion region, uint wl)
    {
        var id = $"add3:{element},{region},{wl}";
        return region switch
        {
            ServerRegion.America => RespondWithModalAsync<AmericaModal>(id),
            ServerRegion.Europe  => RespondWithModalAsync<EuropeModal>(id),
            ServerRegion.Asia    => RespondWithModalAsync<AsiaModal>(id),
            ServerRegion.SAR     => RespondWithModalAsync<SARModal>(id),
            _                    => RespondAsync("Invalid region", ephemeral: true)
        };
    }

    [ModalInteraction("add3:*,*,*", true)]
    public Task AddModalAsync(Element element, ServerRegion region, uint wl, AmericaModal modal)
        => AddAsync(element, region, wl, modal.UID, modal.Length);

    [ModalInteraction("add3:*,*,*", true)]
    public Task AddModalAsync(Element element, ServerRegion region, uint wl, EuropeModal modal)
        => AddAsync(element, region, wl, modal.UID, modal.Length);

    [ModalInteraction("add3:*,*,*", true)]
    public Task AddModalAsync(Element element, ServerRegion region, uint wl, AsiaModal modal)
        => AddAsync(element, region, wl, modal.UID, modal.Length);

    [ModalInteraction("add3:*,*,*", true)]
    public Task AddModalAsync(Element element, ServerRegion region, uint wl, SARModal modal)
        => AddAsync(element, region, wl, modal.UID, modal.Length);

    [SlashCommand("export", "Export data")]
    [Services.Core.Preconditions.Interactions.RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ExportAsync()
    {
        static MemoryStream GenerateStreamFromString(string value) => new(Encoding.UTF8.GetBytes(value));

        var export = new Export(LookingUsers.Values, CachedBoxes.Values);
        var json = JsonSerializer.Serialize(export, EvaluationResult.SerializerOptions);
        await RespondWithFileAsync(GenerateStreamFromString(json), "export.json");
    }

    [SlashCommand("force-remove", "Remove a box from the list")]
    [Services.Core.Preconditions.Interactions.RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ForceRemoveBoxAsync(IUser user)
    {
        var boxes = CachedBoxes.Where(b => b.Key == user.Id).ToList();
        if (boxes.Any())
        {
            foreach (var box in boxes)
            {
                CachedBoxes.Remove(box.Key, out _);
            }

            var builder = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## ✅ Removed boxes ({boxes.Count})\nFor {MentionUtils.MentionUser(user.Id)}")
                    .WithAccentColor(0x422a54));

            foreach (var box in boxes.Take(10))
                builder.WithContainer(BuildBoxContainer(box.Value, "Box removed"));

            await RespondAsync(components: builder.Build(), ephemeral: true);

            await RefreshAsync();
        }
        else await RespondAsync("You did not have your box added to the list.", ephemeral: true);
    }

    [SlashCommand("import", "Import data")]
    [Services.Core.Preconditions.Interactions.RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ImportAsync(Attachment attachment)
    {
        var stream = await http.GetStreamAsync(attachment.Url);
        var data = await JsonSerializer.DeserializeAsync<Export>(stream);
        if (data is null)
        {
            await RespondAsync("Invalid data", ephemeral: true);
            return;
        }

        var looking = data.Looking.ToDictionary(l => (l.User.Id, l.Type));
        var boxes = data.Boxes.ToDictionary(b => b.Id);

        cache.Set(LookingKey, new ConcurrentDictionary<(ulong Id, Element Element), Looking>(looking));
        cache.Set(BoxKey, new ConcurrentDictionary<ulong, Box>(boxes));
        await RespondAsync("Imported data", ephemeral: true);
    }

    [SlashCommand("list", "List the boxes available")]
    public async Task ListBoxesAsync(
        Element element, ServerRegion? region = null,
        uint? worldLevel = null)
    {
        if (region is null && Context.User is IGuildUser user)
        {
            var roles = user.RoleIds.Intersect(RegionRoles.Keys).ToList();
            if (roles.Count == 1) region = RegionRoles[roles[0]];
        }

        if (worldLevel is > 8 or < 1)
        {
            await RespondAsync("Invalid World Level", ephemeral: true);
            return;
        }

        _ = RemoveExpired();

        var matches = CachedBoxes.Values
            .Where(b => b.Type == element)
            .Where(b => region is null || b.Region == region)
            .Where(b => (worldLevel ?? 8) >= b.WorldLevel)
            .OrderByDescending(b => b.Added)
            .ToList();

        var addButton = new ButtonBuilder(
            "Add my box to the list",
            worldLevel is null
                ? region is null ? $"add0:{element}" : $"add1:{element},{region}"
                : $"add2:{element},{region},{worldLevel}",
            ButtonStyle.Success);

        var lookButton = new ButtonBuilder(
            "These boxes do not work for me",
            worldLevel is null
                ? region is null ? $"look0:true,{element}" : $"look1:true,{element},{region}"
                : $"look2:true,{element},{region},{worldLevel}",
            ButtonStyle.Danger);

        if (matches.Count == 0)
        {
            var emote = BoxEmotes[element];
            var emojiUrl = $"https://cdn.discordapp.com/emojis/{emote.Id}.png";

            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithSection(
                        [new TextDisplayBuilder($"## No boxes found\nTry adding your own box or start looking.")],
                        new ThumbnailBuilder(new UnfurledMediaItemProperties(emojiUrl)))
                    .WithAccentColor(0x422a54))
                .WithActionRow(new ActionRowBuilder()
                    .WithButton(addButton)
                    .WithButton(lookButton))
                .Build();

            await RespondAsync(components: components, ephemeral: true);
            return;
        }

        const int boxesPerPage = 6;
        var pageCount = Math.Max(1, (int)Math.Ceiling((double)matches.Count / boxesPerPage));

        var paginator = new ComponentPaginatorBuilder()
            .WithUsers(Context.User)
            .WithPageCount(pageCount)
            .WithPageFactory(GeneratePage)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .Build();

        await interactive.SendPaginatorAsync(paginator, Context.Interaction,
            responseType: InteractionResponseType.ChannelMessageWithSource,
            ephemeral: true,
            resetTimeoutOnInput: true);

        IPage GeneratePage(IComponentPaginator p)
        {
            var emote = BoxEmotes[element];
            var emojiUrl = $"https://cdn.discordapp.com/emojis/{emote.Id}.png";
            var filterLine = region is null && worldLevel is null
                ? "-# Showing all matches"
                : $"-# Filters: {(region is null ? "Any region" : region.ToString())} • WL ≤ {(worldLevel ?? 8)}";

            var header = new SectionBuilder()
                .WithTextDisplay(
                    $"## {element.Humanize(LetterCasing.Title)} boxes ({matches.Count})\n{filterLine}")
                .WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(emojiUrl)));

            var pageItems = matches
                .Skip(p.CurrentPageIndex * boxesPerPage)
                .Take(boxesPerPage)
                .ToList();

            var container = new ContainerBuilder()
                .WithSection(header)
                .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

            for (var i = 0; i < pageItems.Count; i++)
            {
                var box = pageItems[i];
                var text = new StringBuilder()
                    .AppendLine($"### `{box.Id}` • WL{box.WorldLevel}")
                    .AppendLine($"User: {MentionUtils.MentionUser(box.User.Id)} • Region: {box.Region}")
                    .AppendLine($"Added: <t:{box.Added.ToUnixTimeSeconds()}:R>")
                    .ToString()
                    .Trim();

                container.WithSection(new SectionBuilder().WithTextDisplay(text));

                if (i < pageItems.Count - 1)
                    container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
            }

            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
            container.WithActionRow(new ActionRowBuilder()
                .WithButton(new ButtonBuilder(addButton.Label, addButton.CustomId, addButton.Style,
                    emote: addButton.Emote, isDisabled: p.ShouldDisable()))
                .WithButton(new ButtonBuilder(lookButton.Label, lookButton.CustomId, lookButton.Style,
                    emote: lookButton.Emote, isDisabled: p.ShouldDisable())));

            container.WithActionRow(new ActionRowBuilder()
                .AddPreviousButton(p, "◀", ButtonStyle.Secondary)
                .AddJumpButton(p, $"{p.CurrentPageIndex + 1} / {p.PageCount}")
                .AddNextButton(p, "▶", ButtonStyle.Secondary)
                .AddStopButton(p, "Close", ButtonStyle.Danger));

            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay($"-# Page {p.CurrentPageIndex + 1} of {p.PageCount}")
                .WithAccentColor(0x422a54);

            var components = new ComponentBuilderV2().WithContainer(container).Build();
            return new PageBuilder()
                .WithComponents(components)
                .WithAllowedMentions(AllowedMentions.None)
                .Build();
        }
    }

    [ComponentInteraction("list:*", true)]
    public Task ListBoxesAsync(string element) => ListBoxesAsync(element.ToLowerInvariant() switch
    {
        "anemo"   => Anemo,
        "cryo"    => Cryo,
        "dendro"  => Dendro,
        "electro" => Electro,
        "geo"     => Geo,
        "hydro"   => Hydro,
        "pyro"    => Pyro,
        _         => throw new InvalidOperationException("Invalid element!")
    });

    [SlashCommand("looking", "Get pinged if someone comes with a box that you need.")]
    public async Task LookingAsync(
        Element element, ServerRegion region,
        [Discord.Interactions.Summary(description: "Your world level. Must be 1-8.")]
        uint worldLevel,
        [Discord.Interactions.Summary(description: "Force the addition of your notification")]
        bool forceAdd = false)
    {
        if (worldLevel is > 8 or < 1)
        {
            await RespondAsync("Invalid World Level", ephemeral: true);
            return;
        }

        var found = CachedBoxes.Values
            .Where(l => element == l.Type)
            .Where(l => region == l.Region)
            .Where(l => worldLevel >= l.WorldLevel)
            .ToList();

        if (found.Any() && !forceAdd)
            await ListBoxesAsync(element, region, worldLevel);
        else
        {
            var user = new BoxUser(Context.User.Id, Context.User.ToString());
            var looking = new Looking(user, region, element, worldLevel);
            LookingUsers[(Context.User.Id, element)] = looking;

            await DeferAsync(true);
            var components = BuildBoxComponents(looking, "✅ Added your looking status");
            await ModifyOriginalResponseAsync(m =>
            {
                m.Components = components;
                m.Embeds = Array.Empty<Embed>();
            });
            await RefreshAsync();
        }
    }

    [ComponentInteraction("look-interactive:*,*", true)]
    public async Task LookingInteractiveAsync(bool restart, bool force)
    {
        const string message = "Hello, I will guide you through finding a box. What element are you looking for?";

        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\n{message}")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var buttons = BoxEmotes.Select(kv =>
            new ButtonBuilder(kv.Key.ToString(), $"look0:{force},{kv.Key}", ButtonStyle.Secondary, emote: kv.Value)).ToList();

        var builder = new ComponentBuilderV2().WithContainer(container);
        builder.WithActionRow(new ActionRowBuilder().WithComponents(buttons.Take(4)));
        builder.WithActionRow(new ActionRowBuilder().WithComponents(buttons.Skip(4)));

        var components = builder.Build();
        if (restart)
        {
            await DeferAsync();
            await ModifyOriginalResponseAsync(m =>
            {
                m.Components = components;
                m.Embeds = Array.Empty<Embed>();
            });
        }
        else
        {
            await RespondAsync(
                ephemeral: true,
                components: components);
        }
    }

    [ComponentInteraction("look0:*,*", true)]
    public async Task LookingInteractiveAsync(bool force, Element element)
    {
        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\nSo the element is **{element}**. What is your region?")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var row = new ActionRowBuilder();
        foreach (var region in Enum.GetValues<ServerRegion>())
            row.WithButton(new ButtonBuilder(region.ToString(), $"look1:{force},{element},{region}", ButtonStyle.Secondary));

        row.WithButton(new ButtonBuilder("Restart", $"look-interactive:true,{force}", ButtonStyle.Danger));

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(row)
            .Build();

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Embeds = Array.Empty<Embed>();
        });
    }

    [ComponentInteraction("look1:*,*,*", true)]
    public async Task LookingInteractiveAsync(bool force, Element element, ServerRegion region)
    {
        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\nLooking for **{element}** in **{region}**. What is your world level?")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithAccentColor(0x422a54);

        var first = new ActionRowBuilder();
        for (var i = 1; i <= 5; i++)
            first.WithButton(new ButtonBuilder($"{i}", $"look2:{force},{element},{region},{i}", ButtonStyle.Secondary));

        var second = new ActionRowBuilder();
        for (var i = 6; i <= 8; i++)
            second.WithButton(new ButtonBuilder($"{i}", $"look2:{force},{element},{region},{i}", ButtonStyle.Secondary));
        second.WithButton(new ButtonBuilder("Restart", $"look-interactive:true,{force}", ButtonStyle.Danger));

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(first)
            .WithActionRow(second)
            .Build();

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Embeds = Array.Empty<Embed>();
        });
    }

    [ComponentInteraction("look2:*,*,*,*", true)]
    public Task LookingInteractiveAsync(bool force, Element element, ServerRegion region, uint wl)
        => LookingAsync(element, region, wl, force);

    [ComponentInteraction("not-looking", true)]
    [SlashCommand("not-looking", "Remove your looking status.")]
    public async Task NotLookingAsync()
    {
        var boxes = LookingUsers
            .Where(u => u.Key.Id == Context.User.Id)
            .ToList();

        if (boxes.Any())
        {
            foreach (var box in boxes)
            {
                LookingUsers.Remove(box.Key, out _);
            }

            await RespondAsync("Removed your looking status.", ephemeral: true);
            await RefreshAsync();
        }
        else await RespondAsync("You were not looking for a box.", ephemeral: true);
    }

    [ComponentInteraction("remove", true)]
    [SlashCommand("remove", "Remove yourself to the list of marvelous merchandise.")]
    public async Task RemoveAsync()
    {
        if (CachedBoxes.TryRemove(Context.User.Id, out var box))
        {
            var components = BuildBoxComponents(box, "✅ Removed your box");
            await RespondAsync(components: components, ephemeral: true);
            await RefreshAsync();
        }
        else await RespondAsync("You did not have your box added to the list.", ephemeral: true);
    }

    [SlashCommand("unregister-commands", "Unregister the slash commands to the guild")]
    [Services.Core.Preconditions.Interactions.RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task UnregisterCommandsAsync()
    {
        cache.Remove(BoxKey);
        cache.Remove(ExpiredSemaphoreKey);
        cache.Remove(IsBottomKey);
        cache.Remove(LastMessageKey);
        cache.Remove(LookingKey);
        cache.Remove(SemaphoreKey);
        cache.Remove(TokenSourceKey);
        cache.Remove(ChannelKey);

        var empty = Array.Empty<ApplicationCommandProperties>();
        await client.Rest.BulkOverwriteGuildCommands(empty, Context.Guild.Id);
        await RespondAsync("Unregistered commands", ephemeral: true);
    }

    private EmbedBuilder GetEmbed(IBox? box)
    {
        if (box is null) throw new NullReferenceException("Box was null.");
        var emote = BoxEmotes[box.Type];
        var user = GetGuild().GetUser(box.User.Id);

        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user)
            .WithTitle(box.Type.Humanize())
            .AddField("Region", box.Region.Humanize(), true)
            .AddField("Type", box.Type.Humanize(), true)
            .AddField("WL", box.WorldLevel, true)
            .WithThumbnailUrl($"https://cdn.discordapp.com/emojis/{emote.Id}.png");

        return box is Box b
            ? embed
                .AddField("ID", $"{Format.Code($"{b.Id}")} WL{b.WorldLevel}", true)
                .AddField("Added", b.Added.ToUniversalTimestamp())
                .AddField("Expiration", b.Expiry.ToUniversalTimestamp())
            : embed;
    }

    private static EmbedBuilder Liben() => new EmbedBuilder()
        .WithAuthor("Liben", LibenImage)
        .WithThumbnailUrl(LibenImage);

    private MessageComponent BuildBoxComponents(IBox box, string header)
        => new ComponentBuilderV2()
            .WithContainer(BuildBoxContainer(box, header))
            .Build();

    private ContainerBuilder BuildBoxContainer(IBox box, string header)
    {
        var emote = BoxEmotes[box.Type];
        var emojiUrl = $"https://cdn.discordapp.com/emojis/{emote.Id}.png";
        var user = GetGuild().GetUser(box.User.Id);
        var userLine = user is null ? $"{MentionUtils.MentionUser(box.User.Id)}" : $"{user} {user.Mention}";

        var text = new StringBuilder()
            .AppendLine($"## {header}")
            .AppendLine($"### {box.Type.Humanize(LetterCasing.Title)}")
            .AppendLine($"**User:** {userLine}")
            .AppendLine($"**Region:** {box.Region.Humanize(LetterCasing.Title)}")
            .AppendLine($"**WL:** {box.WorldLevel}")
            .ToString()
            .Trim();

        if (box is Box b)
        {
            text += "\n" + new StringBuilder()
                .AppendLine($"**UID:** `{b.Id}`")
                .AppendLine($"**Added:** <t:{b.Added.ToUnixTimeSeconds()}:R>")
                .AppendLine($"**Expires:** <t:{b.Expiry.ToUnixTimeSeconds()}:R>")
                .ToString()
                .TrimEnd();
        }

        var section = new SectionBuilder()
            .WithTextDisplay(text)
            .WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(emojiUrl)));

        return new ContainerBuilder()
            .WithSection(section)
            .WithAccentColor(0x422a54);
    }

    private static EmbedFieldBuilder GetPage(Box box) => new EmbedFieldBuilder()
        .WithName($"{Format.Code($"{box.Id}")} WL{box.WorldLevel}")
        .WithValue(new StringBuilder()
            .AppendLine($"User: {MentionUtils.MentionUser(box.User.Id)} {box.User.Username}")
            .AppendLine($"Region: {box.Region}")
            .AppendLine($"Type: {box.Type}")
            .AppendLine($"Added: {box.Added.Humanize()}")
            .ToString());

    private SocketGuild GetGuild() => Context?.Guild ?? client.GetGuild(GuildId);

    private async Task RefreshAsync()
    {
        if (Channel is null) return;
        try
        {
            await Semaphore.WaitAsync();
            await SendMessageAsync();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task RemoveExpired()
    {
        try
        {
            await ExpiredSemaphore.WaitAsync();
            var expired = CachedBoxes.Where(b => DateTimeOffset.Now >= b.Value.Expiry);
            foreach (var box in expired)
            {
                try
                {
                    CachedBoxes.Remove(box.Key, out var removed);
                    if (removed is null) continue;
                    var user = await client.GetUserAsync(removed.User.Id);
                    var dm = await user.CreateDMChannelAsync();

                    var components = BuildBoxComponents(removed, "⏱️ Your box expired and was removed");
                    await dm.SendMessageAsync(components: components);
                }
                catch
                {
                    // Ignored
                }
            }
        }
        finally
        {
            ExpiredSemaphore.Release();
        }
    }

    private async Task ReplyFoundAsync(IBox box)
    {
        var found = LookingUsers.Values
            .Where(l => l.Type == box.Type)
            .Where(l => l.Region == box.Region)
            .Where(l => l.WorldLevel >= box.WorldLevel)
            .ToList();

        if (found.Any())
        {
            await Context.Channel.SendMessageAsync(
                components: new ComponentBuilderV2()
                    .WithContainer(new ContainerBuilder()
                        .WithTextDisplay($"## Someone has appeared with your box\n{found.Humanize(l => MentionUtils.MentionUser(l.User.Id))}")
                        .WithAccentColor(0x422a54))
                    .WithContainer(BuildBoxContainer(box, "Box details"))
                    .WithActionRow(new ActionRowBuilder().WithButton(
                        new ButtonBuilder("This box does not work for me", "look-interactive:false,true",
                            ButtonStyle.Danger)))
                    .Build());

            foreach (var looking in found)
            {
                LookingUsers.Remove((looking.User.Id, looking.Type), out _);
            }
        }
    }

    private async Task SendMessageAsync()
    {
        _ = RemoveExpired();
        TokenSource.Cancel();
        TokenSource = new CancellationTokenSource();

        var intro = LibenEmbed.Build();
        var introText = intro.Description ?? "Marvelous Merchandise";

        var container = new ContainerBuilder()
            .WithSection(
                [new TextDisplayBuilder($"## Marvelous Merchandise\n{introText}")],
                new ThumbnailBuilder(new UnfurledMediaItemProperties(LibenImage)))
            .WithMediaGallery([
                new MediaGalleryItemProperties(new UnfurledMediaItemProperties(EventImage), "Event")
            ])
            .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
            .WithTextDisplay("-# The buttons below show [boxes available | people looking for this]")
            .WithAccentColor(0x422a54);

        var builder = new ComponentBuilderV2().WithContainer(container);

        foreach (var chunk in BoxEmotes.ToList().Chunk(3))
        {
            var row = new ActionRowBuilder();
            foreach (var (element, emote) in chunk)
            {
                var boxes = CachedBoxes.Values.Count(b => b.Type == element);
                var looking = LookingUsers.Values.Count(b => b.Type == element);

                row.WithButton(new ButtonBuilder(
                    $"Has: {boxes} | Need: {looking}",
                    $"list:{element}",
                    ButtonStyle.Secondary,
                    emote: emote));
            }

            builder.WithActionRow(row);
        }

        builder.WithActionRow(new ActionRowBuilder()
            .WithButton(new ButtonBuilder("I have a…", "add-interactive:false", ButtonStyle.Success))
            .WithButton(new ButtonBuilder("I need a…", "look-interactive:false,false", ButtonStyle.Success)));

        var components = builder.Build();

        if (LastMessage is not null && IsBottom)
        {
            await LastMessage.ModifyAsync(m =>
            {
                m.Components = components;
                m.Embeds = Array.Empty<Embed>();
            }, new RequestOptions { CancelToken = TokenSource.Token });
        }
        else
        {
            LastMessage?.DeleteAsync();
            LastMessage = await Channel!.SendMessageAsync(
                components: components,
                options: new RequestOptions { CancelToken = TokenSource.Token });
            IsBottom = true;
        }
    }

    public record Export(IEnumerable<Looking> Looking, IEnumerable<Box> Boxes);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class LibenModal : IModal
    {
        [RequiredInput]
        [InputLabel("Enter your UID")]
        public abstract ulong UID { get; set; }

        [RequiredInput(false)]
        [InputLabel("Delete box after... 00h00m")]
        [ModalTextInput("length", TextInputStyle.Short, "30m")]
        public TimeSpan? Length { get; set; }

        public string Title => "Marvelous Merchandise";
    }

    public class AmericaModal : LibenModal
    {
        [ModalTextInput("uid", TextInputStyle.Short, "600......", 1, 9)]
        public override ulong UID { get; set; }
    }

    public class EuropeModal : LibenModal
    {
        [ModalTextInput("uid", TextInputStyle.Short, "700......", 1, 9)]
        public override ulong UID { get; set; }
    }

    public class AsiaModal : LibenModal
    {
        [ModalTextInput("uid", TextInputStyle.Short, "800......", 1, 9)]
        public override ulong UID { get; set; }
    }

    public class SARModal : LibenModal
    {
        [ModalTextInput("uid", TextInputStyle.Short, "900......", 1, 9)]
        public override ulong UID { get; set; }
    }
}

public record Looking(
    BoxUser User,
    ServerRegion Region,
    Element Type,
    uint WorldLevel) : IBox;

public interface IBox
{
    BoxUser User { get; }

    Element Type { get; }

    ServerRegion Region { get; }

    uint WorldLevel { get; }
}

public record BoxUser(ulong Id, string Username);

public class Box : IBox
{
    public enum Element
    {
        Pyro,
        Hydro,
        Anemo,
        Electro,
        Dendro,
        Cryo,
        Geo
    }

    public Box() { }

    public Box(
        IUser user, ulong id, uint wl, Element element, ServerRegion region,
        TimeSpan? deleteAfter)
    {
        User       = new BoxUser(user.Id, user.ToString()!);
        Id         = id;
        Type       = element;
        Region     = region;
        Expiry     = Added + (deleteAfter ?? TimeSpan.FromMinutes(30));
        WorldLevel = wl;
    }

    public DateTimeOffset Added { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset Expiry { get; set; }

    public ulong Id { get; set; }

    public BoxUser User { get; set; } = null!;

    public Element Type { get; set; }

    public ServerRegion Region { get; set; }

    public uint WorldLevel { get; set; }
}