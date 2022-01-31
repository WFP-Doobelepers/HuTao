using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;

namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public abstract class Reprimand : IModerationAction, IGuildUserEntity
{
    protected Reprimand() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected Reprimand(ReprimandDetails details)
    {
        Status = ReprimandStatus.Added;

        UserId  = details.User.Id;
        GuildId = details.Guild.Id;

        Action  = details;
        Trigger = details.Trigger;
    }

    public Guid Id { get; set; }

    public Guid? TriggerId { get; set; }

    public virtual GuildEntity? Guild { get; set; }

    public virtual GuildUserEntity? User { get; set; }

    public virtual ModerationAction? ModifiedAction { get; set; }

    public ReprimandStatus Status { get; set; }

    public virtual Trigger? Trigger { get; set; }

    public ulong GuildId { get; set; }

    public virtual ModerationAction? Action { get; set; }

    public ulong UserId { get; set; }

    public static implicit operator ReprimandResult(Reprimand reprimand) => new(reprimand);
}

public class ReprimandConfiguration : IEntityTypeConfiguration<Reprimand>
{
    public void Configure(EntityTypeBuilder<Reprimand> builder) => builder.AddUserNavigation(r => r.User);
}