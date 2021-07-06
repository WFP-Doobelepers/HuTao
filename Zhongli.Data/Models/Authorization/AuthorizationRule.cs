using System;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Authorization
{
    public abstract class AuthorizationRule : IModerationAction
    {
        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

        public ModerationAction Action { get; set; }
    }
}