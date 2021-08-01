using System;

namespace Zhongli.Data.Models.Authorization
{
    [Flags]
    public enum AuthorizationScope
    {
        None          = 0,
        All           = 1 << 0,
        Warning       = 1 << 1,
        Mute          = 1 << 2,
        Kick          = 1 << 3,
        Ban           = 1 << 4,
        Configuration = 1 << 5,
        Moderator     = Warning | Mute | Kick | Ban,
        Helper        = Warning | Mute
    }
}