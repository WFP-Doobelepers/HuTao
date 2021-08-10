using System;

namespace Zhongli.Data.Models.Logging
{
    [Flags]
    public enum ReprimandNoticeType
    {
        All     = 0,
        Ban     = 1 << 0,
        Censor  = 1 << 1,
        Kick    = 1 << 2,
        Mute    = 1 << 3,
        Notice  = 1 << 4,
        Warning = 1 << 5
    }
}