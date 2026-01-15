using System.Collections.Generic;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Moderation;

namespace HuTao.Services.Utilities;

public static class ReprimandBadgeHelper
{
    private static readonly Dictionary<System.Type, string> TypeColors = new()
    {
        [typeof(Warning)] = "FFA500",
        [typeof(Mute)] = "DC143C",
        [typeof(HardMute)] = "8B0000",
        [typeof(Ban)] = "2C3E50",
        [typeof(Kick)] = "FF6347",
        [typeof(Timeout)] = "FF7F50",
        [typeof(Notice)] = "3498DB",
        [typeof(Note)] = "95A5A6",
        [typeof(Censored)] = "9B59B6",
        [typeof(Filtered)] = "E67E22",
        [typeof(RoleReprimand)] = "1ABC9C"
    };

    private static readonly Dictionary<ReprimandStatus, string> StatusColors = new()
    {
        [ReprimandStatus.Added] = "2ECC71",
        [ReprimandStatus.Updated] = "3498DB",
        [ReprimandStatus.Expired] = "95A5A6",
        [ReprimandStatus.Pardoned] = "1ABC9C",
        [ReprimandStatus.Deleted] = "E74C3C",
        [ReprimandStatus.Unknown] = "7F8C8D"
    };

    public static string GetTypeBadges(Reprimand reprimand)
    {
        return reprimand.GetTitle(showId: false);
    }

    public static string GetStatusBadge(ReprimandStatus status)
    {
        return status.ToString();
    }

    public static string GetCombinedBadges(Reprimand reprimand)
    {
        var typeBadges = GetTypeBadges(reprimand);
        var statusBadge = GetStatusBadge(reprimand.Status);
        
        return $"{typeBadges} {statusBadge}";
    }

    public static string GetTypeOnly(Reprimand reprimand)
    {
        return GetTypeBadges(reprimand);
    }

    public static string GetCompactBadge(Reprimand reprimand, bool includeStatus = false)
    {
        var type = GetTypeBadges(reprimand);
        if (!includeStatus)
            return type;

        return $"{type} {GetStatusBadge(reprimand.Status)}";
    }

    public static string GetTypeColor(Reprimand reprimand)
    {
        var type = reprimand.GetType();
        return TypeColors.TryGetValue(type, out var color) ? color : "95A5A6";
    }

    public static string GetStatusColor(ReprimandStatus status)
    {
        return StatusColors.TryGetValue(status, out var color) ? color : "95A5A6";
    }
}

