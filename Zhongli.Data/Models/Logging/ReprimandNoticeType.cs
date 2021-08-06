using System;

namespace Zhongli.Data.Models.Logging
{
    [Flags]
    public enum ReprimandNoticeType
    {
        All     = 0,
        Ban     = 1 << 0,
        Kick    = 1 << 1,
        Mute    = 1 << 2,
        Notice  = 1 << 3,
        Warning = 1 << 4
    }
}