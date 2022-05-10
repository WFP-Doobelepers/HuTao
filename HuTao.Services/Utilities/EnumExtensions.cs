using System;
using System.Collections.Generic;
using Discord;
using HuTao.Data.Models.Moderation.Logging;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig;

namespace HuTao.Services.Utilities;

public static class EnumExtensions
{
    private static readonly GenericBitwise<LogReprimandType> LogReprimandTypeBitwise = new();
    private static readonly GenericBitwise<LogReprimandStatus> LogReprimandStatusBitwise = new();
    private static readonly GenericBitwise<ModerationLogOptions> ModerationLogOptionsBitwise = new();
    private static readonly GenericBitwise<GuildPermission> GuildPermissionBitwise = new();

    public static GuildPermissions ToGuildPermissions(this IEnumerable<GuildPermission> permissions)
        => new((uint) GuildPermissionBitwise.Or(permissions));

    public static LogReprimandStatus SetValue(this LogReprimandStatus options, LogReprimandStatus flag,
        bool? state)
        => LogReprimandStatusBitwise.SetValue(options, flag, state);

    public static LogReprimandType SetValue(this LogReprimandType options, LogReprimandType flag,
        bool? state)
        => LogReprimandTypeBitwise.SetValue(options, flag, state);

    public static ModerationLogOptions SetValue(this ModerationLogOptions options, ModerationLogOptions flag,
        bool? state)
        => ModerationLogOptionsBitwise.SetValue(options, flag, state);

    public static T SetValue<T>(this GenericBitwise<T> generic, T @enum, T flag, bool? state)
        where T : Enum
    {
        if (state is null)
            return generic.Xor(@enum, flag);

        return state.Value
            ? generic.Or(@enum, flag)
            : generic.And(@enum, generic.Not(flag));
    }
}