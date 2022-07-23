using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Discord;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class InviteExclusion : ModerationExclusion, IGuildEntity
{
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public InviteExclusion(GuildEntity guild, AutoConfiguration? config) : base(config) { Guild = guild; }

    protected InviteExclusion() { }

    public virtual GuildEntity Guild { get; set; } = null!;

    [Column(nameof(GuildId))]
    public ulong GuildId { get; set; }

    public bool Judge(IInvite invite) => Guild.Id == invite.GuildId;
}