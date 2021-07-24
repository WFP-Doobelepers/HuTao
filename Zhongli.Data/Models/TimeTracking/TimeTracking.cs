using System;

namespace Zhongli.Data.Models.TimeTracking
{
    public abstract class TimeTracking
    {
        public Guid Id { get; set; }

        public ulong GuildId { get; set; }
    }
}