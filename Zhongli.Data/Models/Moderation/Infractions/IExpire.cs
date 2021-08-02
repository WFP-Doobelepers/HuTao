using System;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public interface IExpire : ILength
    {
        public Guid Id { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public DateTimeOffset StartedAt { get; set; }
    }
}