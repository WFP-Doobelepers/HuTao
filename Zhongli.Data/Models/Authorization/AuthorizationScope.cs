using System;

namespace Zhongli.Data.Models.Authorization
{
    [Flags]
    public enum AuthorizationScope
    {
        None    = 0b_00000000,
        All     = 0b_00000001,
        Warning = 0b_00000010
    }
}