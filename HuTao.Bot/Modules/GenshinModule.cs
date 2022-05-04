using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using HuTao.Data.Config;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Messages;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using Mapster.Utils;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using static HuTao.Bot.Modules.Box;
using static HuTao.Bot.Modules.Box.Element;
using static HuTao.Services.TimeTracking.GenshinTimeTrackingService;

namespace HuTao.Bot.Modules;

[DontAutoRegister]
[Group("liben", "Marvelous Merchandise Commands")]
public class GenshinModule :
    InteractionModuleBase<SocketInteractionContext>,
    INotificationHandler<MessageReceivedNotification>
{
    private const string BoxKey = $"{nameof(GenshinModule)}.{nameof(CachedBoxes)}";
    private const string LookingKey = $"{nameof(GenshinModule)}.{nameof(LookingUsers)}";
    private const string EventImage = "https://pbs.twimg.com/media/FR1JQa-aUAA_rAo.png";
    private const string LibenImage = "https://static.wikia.nocookie.net/gensin-impact/images/1/10/NPC_Liben.png";
    private static readonly SemaphoreSlim ExpiredSemaphore = new(1, 1);
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static CancellationTokenSource _tokenSource = new();
    private static IUserMessage? _lastMessage;
    private static bool _isBottom;

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
        .WithDescription(new StringBuilder()
            .AppendLine("Hey guys, glad to see you here!")
            .AppendLine("To keep things streamlined, you can use the following commands")
            .AppendLine()
            .AppendLine(Format.Bold("Commands"))
            .AppendLine($"{Command("add")} to add your box to the list")
            .AppendLine($"{Command("remove")} to remove your box to the list, or click the button below")
            .AppendLine("Entries are automatically removed after 6 hours")
            .AppendLine()
            .AppendLine($"{Command("list")} to view pyro boxes, or **click one of the buttons below**")
            .AppendLine($"{Command("looking")} to get pinged if someone with a box appears")
            .AppendLine($"{Command("not-looking")} to get your looking status removed")
            .AppendLine()
            .AppendLine(Format.Bold("Buttons"))
            .AppendLine($"{Format.Bold("Add my box")} to add your box to the list")
            .AppendLine($"{Format.Bold("Remove my box")} to remove your box to the list")
            .AppendLine((string?) $"{Format.Bold("I need a box")} to get pinged if someone with a box appears")
            .AppendLine((string?) $"{Format.Bold("Not looking anymore")} to get your looking status removed")
            .ToString())
        .WithImageUrl(EventImage)
        .WithFooter("The buttons below show Total | Looking");

    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly InteractiveService _interactive;

    public GenshinModule(IMemoryCache cache, InteractiveService interactive, DiscordSocketClient client)
    {
        _cache       = cache;
        _interactive = interactive;
        _client      = client;
    }

    public ConcurrentDictionary<(ulong Id, Element Element), Looking> LookingUsers
        => _cache.GetOrCreate(LookingKey, _ => new ConcurrentDictionary<(ulong, Element), Looking>());

    public ConcurrentDictionary<ulong, Box> CachedBoxes
        => _cache.GetOrCreate(BoxKey, _ => new ConcurrentDictionary<ulong, Box>());

    private IMessageChannel Channel => _client
        .GetGuild(HuTaoConfig.Configuration.Guild)
        .GetTextChannel(HuTaoConfig.Configuration.Liben);

    [SlashCommand("add", "Add yourself to the list of marvelous merchandise.")]
    public async Task AddAsync(Element element, ServerRegion region,
        [Summary(description: "Your world level. Must be 1-8.")] uint worldLevel,
        [Summary(description: "Your UID in the game.")] ulong id,
        [Summary(description: "Delete your box after this time. Default 30 minutes.")]
        TimeSpan? deleteAfter = null)
    {
        if (worldLevel is > 8 or < 1)
        {
            await RespondAsync("Invalid World Level", ephemeral: true);
            return;
        }

        var box = new Box(Context.User, id, worldLevel, element, region, deleteAfter);
        CachedBoxes[Context.User.Id] = box;

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m => m.Embed = GetEmbed(box).Build());

        await RefreshAsync();
        await ReplyFoundAsync(box);
    }

    [SlashCommand("force-remove", "Remove a box from the list")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task ForceRemoveBoxAsync(IUser user)
    {
        var boxes = CachedBoxes.Where(b => b.Key == user.Id).ToList();
        if (boxes.Any())
        {
            foreach (var box in boxes)
            {
                CachedBoxes.Remove(box.Key, out _);
            }

            await RespondAsync("Removed your box.", ephemeral: true,
                embeds: boxes.Take(10).Select(b => GetEmbed(b.Value).Build()).ToArray());

            await RefreshAsync();
        }
        else await RespondAsync("You did not have your box added to the list.", ephemeral: true);
    }

    [SlashCommand("list", "List the boxes available")]
    public async Task ListBoxesAsync(Element element, ServerRegion? region = null, uint? worldLevel = null)
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
        var emote = BoxEmotes[element];
        var embed = new EmbedBuilder()
            .WithThumbnailUrl($"https://cdn.discordapp.com/emojis/{emote.Id}.png");

        var boxes = CachedBoxes.Values
            .Where(b => b.Type == element)
            .Where(b => region is null || b.Region == region)
            .Where(b => (worldLevel ?? 8) >= b.WorldLevel)
            .OrderByDescending(b => b.Added)
            .Select(GetPage)
            .ToPageBuilders(10, embed)
            .ToList();

        var add = new ButtonBuilder(
            "Add my box to the list",
            worldLevel is null
                ? region is null ? $"add0:{element}" : $"add1:{element},{region}"
                : $"add2:{element},{region},{worldLevel}",
            ButtonStyle.Success);

        var look = new ButtonBuilder(
            "These boxes do not work for me",
            worldLevel is null
                ? region is null ? $"look0:true,{element}" : $"look1:true,{element},{region}"
                : $"look2:true,{element},{region},{worldLevel}",
            ButtonStyle.Danger);

        if (!boxes.Any())
        {
            var components = new ComponentBuilder().WithButton(add).WithButton(look);
            await RespondAsync("There are no boxes found.", ephemeral: true, components: components.Build());
            return;
        }

        var paginator = new StaticPaginatorBuilder()
            .WithDefaultEmotes()
            .WithPages(boxes);

        await _interactive.SendPaginatorAsync(paginator.Build(), Context.Interaction,
            responseType: InteractionResponseType.ChannelMessageWithSource,
            ephemeral: true,
            messageAction: Components);

        void Components(IUserMessage message)
        {
            var components = GetMessageComponents(message).WithButton(add).WithButton(look);
            _ = message switch
            {
                RestInteractionMessage m => m.ModifyAsync(r => r.Components       = components.Build()),
                RestFollowupMessage m    => m.ModifyAsync(r => r.Components       = components.Build()),
                _                        => message.ModifyAsync(r => r.Components = components.Build())
            };
        }

        ComponentBuilder GetMessageComponents(IMessage message)
        {
            var components = ComponentBuilder.FromMessage(message);
            components.ActionRows?.RemoveAll(row => row.Components.Any(c => c.Type is ComponentType.SelectMenu));

            return components;
        }
    }

    [SlashCommand("looking", "Get pinged if someone comes with a box that you need.")]
    public async Task LookingAsync(Element element, ServerRegion region,
        [Summary(description: "Your world level. Must be 1-8.")] uint worldLevel,
        [Summary(description: "Force the addition of your notification")]
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
            var looking = new Looking(Context.User, region, element, worldLevel);
            LookingUsers[(Context.User.Id, element)] = looking;

            await DeferAsync(true);
            await ModifyOriginalResponseAsync(m => m.Embed = GetEmbed(looking).Build());
            await RefreshAsync();
        }
    }

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
            await RespondAsync("Removed your box.", embed: GetEmbed(box).Build(), ephemeral: true);
            await RefreshAsync();
        }
        else await RespondAsync("You did not have your box added to the list.", ephemeral: true);
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel.Id == Channel.Id
            && !notification.Message.Author.IsBot
            && !notification.Message.Author.IsWebhook)
        {
            _isBottom = false;
            await RefreshAsync();
        }
    }

    [ComponentInteraction("add-interactive:*", true)]
    public async Task AddInteractiveAsync(bool restart)
    {
        var component = new ComponentBuilder();
        foreach (var (element, emote) in BoxEmotes)
        {
            var name = element.ToString();
            component.WithButton(name, $"add0:{element}", emote: emote);
        }

        const string message = "Hello, I will guide you through the looking process, what element are you adding?";
        var embed = Liben().WithDescription(message);
        if (restart)
        {
            await DeferAsync(true);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Components = component.Build();
                m.Embed      = embed.Build();
            });
        }
        else
        {
            await RespondAsync(
                ephemeral: true,
                components: component.Build(),
                embed: embed.Build());
        }
    }

    [ComponentInteraction("add0:*", true)]
    public async Task AddInteractiveAsync(Element element)
    {
        var component = new ComponentBuilder();
        foreach (var region in Enum.GetValues<ServerRegion>())
        {
            component.WithButton(region.ToString(), $"add1:{element},{region}");
        }
        component.WithButton("Restart", "add-interactive:true", ButtonStyle.Danger);

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = component.Build();
            m.Embed      = Liben().WithDescription($"So the element is {element}, what is your region?").Build();
        });
    }

    [ComponentInteraction("add1:*,*", true)]
    public async Task AddInteractiveAsync(Element element, ServerRegion region)
    {
        var component = new ComponentBuilder();
        for (var i = 1; i <= 8; i++)
        {
            component.WithButton($"{i}", $"add2:{element},{region},{i}");
        }
        component.WithButton("Restart", "add-interactive:true", ButtonStyle.Danger);

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = component.Build();
            m.Embed = Liben().WithDescription($"Looking for a box of {element} in {region}, what is your WL?").Build();
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

    [ComponentInteraction("list:*", true)]
    public Task ListBoxesAsync(string element) => ListBoxesAsync(Enum<Element>.Parse(element));

    [ComponentInteraction("look-interactive:*,*", true)]
    public async Task LookingInteractiveAsync(bool restart, bool force)
    {
        var component = new ComponentBuilder();
        foreach (var (element, emote) in BoxEmotes)
        {
            component.WithButton(element.ToString(), $"look0:{force},{element}", emote: emote);
        }

        const string message = "Hello, I will guide you through the looking process, what element are you looking for?";
        var embed = Liben().WithDescription(message);
        if (restart)
        {
            await DeferAsync();
            await ModifyOriginalResponseAsync(m =>
            {
                m.Components = component.Build();
                m.Embed      = embed.Build();
            });
        }
        else
        {
            await RespondAsync(
                ephemeral: true,
                components: component.Build(),
                embed: embed.Build());
        }
    }

    [ComponentInteraction("look0:*,*", true)]
    public async Task LookingInteractiveAsync(bool force, Element element)
    {
        var component = new ComponentBuilder();
        foreach (var region in Enum.GetValues<ServerRegion>())
        {
            component.WithButton(region.ToString(), $"look1:{force},{element},{region}");
        }
        component.WithButton("Restart", $"look-interactive:true:{force}", ButtonStyle.Danger);

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = component.Build();
            m.Embed      = Liben().WithDescription($"So the element is {element}, what is your region?").Build();
        });
    }

    [ComponentInteraction("look1:*,*,*", true)]
    public async Task LookingInteractiveAsync(bool force, Element element, ServerRegion region)
    {
        var component = new ComponentBuilder();
        for (var i = 1; i <= 8; i++)
        {
            component.WithButton($"{i}", $"look2:{force},{element},{region},{i}");
        }
        component.WithButton("Restart", $"look-interactive:true:{force}", ButtonStyle.Danger);

        await DeferAsync(true);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = component.Build();
            m.Embed = Liben().WithDescription($"Looking for a box of {element} in {region}, what is your WL?").Build();
        });
    }

    [ComponentInteraction("look2:*,*,*,*", true)]
    public Task LookingInteractiveAsync(bool force, Element element, ServerRegion region, uint wl)
        => LookingAsync(element, region, wl, force);

    private static EmbedBuilder GetEmbed(IBox? box)
    {
        if (box is null) throw new NullReferenceException("Box was null.");
        var emote = BoxEmotes[box.Type];
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(box.User)
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

    private static EmbedFieldBuilder GetPage(Box box) => new EmbedFieldBuilder()
        .WithName($"{Format.Code($"{box.Id}")} WL{box.WorldLevel}")
        .WithValue(new StringBuilder()
            .AppendLine($"User: {box.User.Mention} {box.User}")
            .AppendLine($"Region: {box.Region}")
            .AppendLine($"Type: {box.Type}")
            .AppendLine($"Added: {box.Added.Humanize()}")
            .ToString());

    private static string Command(string name) => Format.Bold(Format.Code($"/liben {name}"));

    private async Task RefreshAsync()
    {
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
                    var dm = await removed.User.CreateDMChannelAsync();
                    await dm.SendMessageAsync("Your box was removed automatically after 30 minutes.",
                        embed: GetEmbed(removed).Build());
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
                $"Someone has appeared with your box {found.Humanize(l => l.User.Mention)}",
                embed: GetEmbed(box).Build());

            foreach (var looking in found)
            {
                LookingUsers.Remove((looking.User.Id, looking.Type), out _);
            }
        }
    }

    private async Task SendMessageAsync()
    {
        _ = RemoveExpired();
        _tokenSource.Cancel();
        _tokenSource = new CancellationTokenSource();

        var component = new ComponentBuilder()
            .WithButton("Add my box", "add-interactive:false", ButtonStyle.Success)
            .WithButton("I need boxes", "look-interactive:false,false", ButtonStyle.Success)
            .WithButton("Remove my box", "remove", ButtonStyle.Danger);

        var row = 0;
        foreach (var (element, emote) in BoxEmotes)
        {
            var boxes = CachedBoxes.Values.Count(b => b.Type == element);
            var looking = LookingUsers.Values.Count(b => b.Type == element);

            component.WithButton($"T: {boxes} | L: {looking}", $"list:{element}",
                emote: emote,
                row: row / 3 + 1);
            row++;
        }

        component.WithButton("Not looking for boxes anymore", "not-looking", ButtonStyle.Secondary, row: 3);

        if (_lastMessage is not null && _isBottom)
        {
            await _lastMessage.ModifyAsync(m =>
            {
                m.Embed      = LibenEmbed.Build();
                m.Components = component.Build();
            }, new RequestOptions { CancelToken = _tokenSource.Token });
        }
        else
        {
            _lastMessage?.DeleteAsync();
            _lastMessage = await Channel.SendMessageAsync(
                embed: LibenEmbed.Build(), components: component.Build(),
                options: new RequestOptions { CancelToken = _tokenSource.Token });
            _isBottom = true;
        }
    }

    public abstract class LibenModal : IModal
    {
        [RequiredInput(false)]
        [InputLabel("Delete box after... 00h00m")]
        [ModalTextInput("length", TextInputStyle.Short, "30m")]
        public TimeSpan Length { get; set; }

        [RequiredInput]
        [InputLabel("Enter your UID")]
        public abstract ulong UID { get; set; }

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

public record Looking(IUser User, ServerRegion Region, Element Type, uint WorldLevel) : IBox;

public interface IBox
{
    Element Type { get; }

    IUser User { get; }

    ServerRegion Region { get; }

    uint WorldLevel { get; }
}

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

    public Box(IUser user, ulong id, uint wl, Element element, ServerRegion region, TimeSpan? deleteAfter)
    {
        User       = user;
        Id         = id;
        Type       = element;
        Region     = region;
        Expiry     = Added + (deleteAfter ?? TimeSpan.FromMinutes(30));
        WorldLevel = wl;
    }

    public ulong Id { get; }

    public DateTimeOffset Added { get; } = DateTimeOffset.Now;

    public DateTimeOffset Expiry { get; }

    public Element Type { get; }

    public IUser User { get; }

    public ServerRegion Region { get; }

    public uint WorldLevel { get; }
}