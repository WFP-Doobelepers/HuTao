using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace HuTao.Services.Channels;

public enum ChannelBrowserFilter
{
    All,
    Text,
    Voice,
    Category
}

public sealed class ChannelBrowserState
{
    private const int PageSize = 10;

    public required string GuildName { get; init; }

    public ChannelBrowserView View { get; set; } = ChannelBrowserView.List;
    public ulong? SelectedChannelId { get; set; }

    public ChannelBrowserFilter Filter { get; set; } = ChannelBrowserFilter.All;
    public string? Search { get; set; }

    public DateTimeOffset? LastUpdated { get; set; }
    public string? Notice { get; set; }

    public List<ChannelEntry> Channels { get; private set; } = new();

    public static ChannelBrowserState Create(SocketGuild guild)
    {
        var state = new ChannelBrowserState { GuildName = guild.Name };
        state.Reload(guild);
        return state;
    }

    public static ChannelBrowserState Create(string guildName, IEnumerable<ChannelEntry> entries)
    {
        return new ChannelBrowserState
        {
            GuildName = guildName,
            Channels = entries.ToList(),
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public void Reload(SocketGuild guild)
    {
        Channels = guild.Channels
            .OfType<SocketGuildChannel>()
            .Select(ChannelEntry.From)
            .OrderByDescending(c => c.Position)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        LastUpdated = DateTimeOffset.UtcNow;
    }

    public int GetPageCount()
    {
        if (View is ChannelBrowserView.Detail)
            return 1;

        return Math.Max(1, (int)Math.Ceiling((double)GetFilteredChannels().Count / PageSize));
    }

    public IReadOnlyList<ChannelEntry> GetPage(int pageIndex)
    {
        var channels = GetFilteredChannels();
        return channels
            .Skip(pageIndex * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public IReadOnlyList<ChannelEntry> GetFilteredChannels()
    {
        IEnumerable<ChannelEntry> result = Channels;

        result = Filter switch
        {
            ChannelBrowserFilter.Text => result.Where(c => c.Kind is ChannelKind.Text or ChannelKind.News),
            ChannelBrowserFilter.Voice => result.Where(c => c.Kind is ChannelKind.Voice or ChannelKind.Stage),
            ChannelBrowserFilter.Category => result.Where(c => c.Kind is ChannelKind.Category),
            _ => result
        };

        var q = Search?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (TryExtractId(q, out var id))
                result = result.Where(c => c.Id == id);
            else
                result = result.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        return result.ToList();
    }

    public ChannelEntry? GetSelected()
    {
        if (SelectedChannelId is null)
            return null;

        return Channels.FirstOrDefault(c => c.Id == SelectedChannelId.Value);
    }

    public void Select(ulong channelId)
    {
        SelectedChannelId = channelId;
        View = ChannelBrowserView.Detail;
    }

    public void Back()
    {
        View = ChannelBrowserView.List;
        SelectedChannelId = null;
    }

    public void ApplySearch(string? query)
    {
        Search = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        Back();
    }

    public void ClearSearch()
    {
        Search = null;
        Back();
    }

    private static bool TryExtractId(string input, out ulong id)
    {
        if (ulong.TryParse(input, out id))
            return true;

        if (input.Contains("<#", StringComparison.Ordinal))
        {
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return ulong.TryParse(digits, out id);
        }

        id = 0;
        return false;
    }
}

public enum ChannelKind
{
    Text,
    News,
    Voice,
    Stage,
    Category,
    Other
}

public sealed record ChannelEntry(
    ulong Id,
    string Name,
    ChannelKind Kind,
    ulong? CategoryId,
    int Position,
    bool IsNsfw,
    int? SlowmodeSeconds,
    int? UserLimit,
    string? Topic)
{
    public string Mention => MentionUtils.MentionChannel(Id);

    public static ChannelEntry From(SocketGuildChannel channel)
    {
        var kind = ChannelKind.Other;
        if (channel is SocketNewsChannel)
            kind = ChannelKind.News;
        else if (channel is SocketStageChannel)
            kind = ChannelKind.Stage;
        else if (channel is SocketVoiceChannel)
            kind = ChannelKind.Voice;
        else if (channel is SocketCategoryChannel)
            kind = ChannelKind.Category;
        else if (channel is SocketTextChannel)
            kind = ChannelKind.Text;

        var nested = channel as INestedChannel;
        var categoryId = nested?.CategoryId;

        var isNsfw = channel is ITextChannel tc && tc.IsNsfw;
        var slowmodeSeconds = channel is ITextChannel text ? (int?)text.SlowModeInterval : null;
        var userLimit = channel is IVoiceChannel voice ? (int?)voice.UserLimit : null;
        var topic = channel is SocketTextChannel stc ? stc.Topic : null;

        return new ChannelEntry(
            channel.Id,
            channel.Name,
            kind,
            categoryId,
            channel.Position,
            isNsfw,
            slowmodeSeconds,
            userLimit,
            topic);
    }
}

