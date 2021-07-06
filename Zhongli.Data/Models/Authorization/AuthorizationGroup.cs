using System;
using System.Collections.Generic;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Authorization
{
    public class AuthorizationGroup
    {
        protected AuthorizationGroup() { }

        public AuthorizationGroup(AuthorizationScope scope, IGuildUser moderator, ICollection<AuthorizationRule> rules)
        {
            Scope      = scope;
            Collection = rules;

            AddedById = moderator.Id;
            GuildId   = moderator.GuildId;
        }

        public Guid Id { get; set; }

        public ulong GuildId { get; set; }

        public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity AddedBy { get; set; }

        public ulong AddedById { get; set; }

        public AuthorizationScope Scope { get; set; }

        public virtual ICollection<AuthorizationRule> Collection { get; set; } = new List<AuthorizationRule>();
    }

    public class AuthorizationGroupConfiguration : IEntityTypeConfiguration<AuthorizationGroup>
    {
        public void Configure(EntityTypeBuilder<AuthorizationGroup> builder)
        {
            builder
                .HasOne(a => a.AddedBy)
                .WithMany().HasForeignKey(a => new { a.AddedById, a.GuildId });
        }
    }
}