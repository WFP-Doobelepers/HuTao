using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Humanizer;
using HuTao.Services.Utilities;

namespace HuTao.Services.Roles;

public sealed class RoleBrowserState
{
    private const int PageSize = 10;

    public required string GuildName { get; init; }

    public RoleBrowserView View { get; set; } = RoleBrowserView.List;
    public ulong? SelectedRoleId { get; set; }

    public string? Filter { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }

    public List<RoleEntry> Roles { get; private set; } = new();

    public static RoleBrowserState Create(SocketGuild guild)
    {
        var state = new RoleBrowserState { GuildName = guild.Name };
        state.Reload(guild);
        return state;
    }

    public static RoleBrowserState Create(string guildName, IEnumerable<RoleEntry> roles)
    {
        return new RoleBrowserState
        {
            GuildName = guildName,
            Roles = roles
                .OrderByDescending(r => r.Position)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public void Reload(SocketGuild guild)
    {
        Roles = guild.Roles
            .OrderByDescending(r => r.Position)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(RoleEntry.From)
            .ToList();

        LastUpdated = DateTimeOffset.UtcNow;
    }

    public int GetPageCount()
    {
        if (View is RoleBrowserView.Detail)
            return 1;

        return Math.Max(1, (int)Math.Ceiling((double)GetFilteredRoles().Count / PageSize));
    }

    public IReadOnlyList<RoleEntry> GetPage(int pageIndex)
    {
        var roles = GetFilteredRoles();
        return roles
            .Skip(pageIndex * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public RoleEntry? GetSelectedRole()
    {
        if (SelectedRoleId is null)
            return null;

        return Roles.FirstOrDefault(r => r.Id == SelectedRoleId.Value);
    }

    public IReadOnlyList<RoleEntry> GetFilteredRoles()
    {
        var filter = Filter?.Trim();
        if (string.IsNullOrWhiteSpace(filter))
            return Roles;

        if (TryExtractId(filter, out var id))
            return Roles.Where(r => r.Id == id).ToList();

        return Roles
            .Where(r => r.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void SelectRole(ulong roleId)
    {
        SelectedRoleId = roleId;
        View = RoleBrowserView.Detail;
    }

    public void BackToList()
    {
        View = RoleBrowserView.List;
        SelectedRoleId = null;
    }

    public void ApplyFilter(string? query)
    {
        Filter = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        BackToList();
    }

    public void ClearFilter()
    {
        Filter = null;
        BackToList();
    }

    private static bool TryExtractId(string input, out ulong id)
    {
        if (ulong.TryParse(input, out id))
            return true;

        if (input.Contains("<@&", StringComparison.Ordinal))
        {
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return ulong.TryParse(digits, out id);
        }

        id = 0;
        return false;
    }
}

public sealed record RoleEntry(
    ulong Id,
    string Name,
    int Position,
    uint Color,
    bool IsHoisted,
    bool IsMentionable,
    bool IsManaged,
    int MemberCount,
    string PermissionsText)
{
    public string Mention => MentionUtils.MentionRole(Id);

    public static RoleEntry From(SocketRole role)
    {
        var permissions = role.Permissions
            .ToList()
            .Select(p => p.Humanize())
            .DefaultIfEmpty("None")
            .ToList();

        var permText = string.Join(", ", permissions).Truncate(700);

        return new RoleEntry(
            role.Id,
            role.Name,
            role.Position,
            role.Color.RawValue,
            role.IsHoisted,
            role.IsMentionable,
            role.IsManaged,
            role.Members.Count(),
            permText);
    }
}

