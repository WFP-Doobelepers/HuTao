using System;
using Discord.Interactions;

namespace HuTao.Data.Models.Moderation.Logging;

[Flags]
public enum LogReprimandType
{
    [Hide] None = 0,
    Ban = 1 << 0,
    Censored = 1 << 1,
    Kick = 1 << 2,
    Mute = 1 << 3,
    Note = 1 << 4,
    Notice = 1 << 5,
    Warning = 1 << 6,
    Role = 1 << 7,
    HardMute = 1 << 8,
    Filtered = 1 << 9,
    All = Ban | Censored | Kick | Mute | Note | Notice | Warning | Role | HardMute | Filtered
}