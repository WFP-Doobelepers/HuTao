using System;

namespace Zhongli.Data.Models.Logging
{
    [Flags]
    public enum LoggingOptions
    {
        Default    = 0,
        Verbose    = 1 << 0,
        Silent     = 1 << 1,
        NotifyUser = 1 << 2,
        Anonymous  = 1 << 3
    }
}