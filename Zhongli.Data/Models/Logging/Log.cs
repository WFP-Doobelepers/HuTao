using System;

namespace Zhongli.Data.Models.Logging
{
    public interface ILog
    {
        DateTimeOffset LogDate { get; set; }
    }
}