using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class UserMentionExclusion : ModerationExclusion, IGuildUserEntity, IJudge<ulong>
{
    protected UserMentionExclusion() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public UserMentionExclusion(GuildUserEntity user, AutoConfiguration? config = null) : base(config) { User = user; }

    public virtual GuildUserEntity User { get; set; } = null!;

    [Column(nameof(GuildId))]
    public ulong GuildId { get; set; }

    public bool Judge(ulong userId) => UserId == userId;

    public ulong UserId { get; set; }
}

public class UserExclusionConfiguration : IEntityTypeConfiguration<UserMentionExclusion>
{
    public void Configure(EntityTypeBuilder<UserMentionExclusion> builder) => builder.AddUserNavigation(u => u.User);
}