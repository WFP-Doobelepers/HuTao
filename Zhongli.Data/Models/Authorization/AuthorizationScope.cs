using System;

namespace Zhongli.Data.Models.Authorization
{
    [Flags]
    public enum AuthorizationScope
    {
        None      = 0b_00000000,
        All       = 0b_00000001,
        Warning   = 0b_00000010,
        Mute      = 0b_00000100,
        Kick      = 0b_00001000,
        Ban       = 0b_00010000,
        Auto      = 0b_00100000,
        Moderator = Warning | Mute | Kick | Ban,
        Helper    = Warning | Mute
    }
}