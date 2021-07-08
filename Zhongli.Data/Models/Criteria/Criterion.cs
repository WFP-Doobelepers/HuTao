using System;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Criteria
{
    public abstract class Criterion : IModerationAction
    {
        public Guid Id { get; set; }

        public ModerationAction Action { get; set; }
    }
}