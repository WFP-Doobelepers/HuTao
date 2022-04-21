using System;

namespace HuTao.Data.Models.Logging;

public interface ILog
{
    DateTimeOffset LogDate { get; set; }
}