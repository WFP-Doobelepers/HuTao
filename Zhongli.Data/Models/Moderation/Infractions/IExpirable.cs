using System;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IExpirable : ILength
    {
        public Guid Id { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset? ExpireAt { get; set; }
    }
}