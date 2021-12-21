using System;

namespace Zhongli.Data.Models.Moderation.Logging;

[Flags]
public enum LogReprimandStatus
{
    None = 0,
    Added = 1 << 0,
    Expired = 1 << 1,
    Updated = 1 << 2,
    Hidden = 1 << 3,
    Deleted = 1 << 4,
    All = Added | Expired | Updated | Hidden | Deleted
}