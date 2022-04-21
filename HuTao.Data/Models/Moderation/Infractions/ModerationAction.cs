using System;
using Discord;
using HuTao.Data.Models.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IModerationAction
{
    public ModerationAction? Action { get; set; }
}

public record ActionDetails(ulong ModeratorId, ulong GuildId, string? Reason = null)
{
    public ActionDetails(IGuildUser user, string? reason) : this(user.Id, user.Guild.Id, reason) { }

    public ModerationAction ToModerationAction() => new(this);

    public static implicit operator ModerationAction(ActionDetails details) => new(details);
}

public class ModerationAction : IGuildUserEntity
{
    protected ModerationAction() { }

    public ModerationAction(IGuildUser user, string? reason = null) : this(new ActionDetails(user, reason)) { }

    public ModerationAction(ActionDetails details)
    {
        Date = DateTimeOffset.UtcNow;

        (UserId, GuildId, Reason) = details;
    }

    public Guid Id { get; set; }

    public DateTimeOffset Date { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual GuildUserEntity Moderator { get; set; } = null!;

    public string? Reason { get; set; }

    public ulong GuildId { get; set; }

    public ulong UserId { get; set; }
}

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder) => builder.AddUserNavigation(m => m.Moderator);
}