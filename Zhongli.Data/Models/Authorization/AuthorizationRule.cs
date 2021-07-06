using System;

namespace Zhongli.Data.Models.Authorization
{
    public abstract class AuthorizationRule
    {
        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    }
}