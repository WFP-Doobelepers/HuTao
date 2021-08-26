using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging
{
    public interface ILog : IGuildEntity
    {
        DateTimeOffset LogDate { get; set; }

        LogType LogType { get; set; }
    }
}