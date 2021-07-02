using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public abstract class AuthorizationRule
    {
        public Guid Id { get; set; }

        public Guid AuthorizationRulesId { get; set; }

        public virtual AuthorizationRules AuthorizationRules { get; set; }

        public ulong GuildId { get; set; }

        public GuildEntity Guild { get; set; }

        public AuthorizationScope Scope { get; set; }

        public DateTimeOffset Date { get; set; }

        public GuildUserEntity AddedBy { get; set; }

        public ulong AddedById { get; set; }
    }

    public class AuthorizationRuleConfiguration : IEntityTypeConfiguration<AuthorizationRule>
    {
        public void Configure(EntityTypeBuilder<AuthorizationRule> builder)
        {
            builder
                .HasOne(a => a.AddedBy)
                .WithMany().HasForeignKey(a => new { a.AddedById, a.GuildId });
        }
    }
}