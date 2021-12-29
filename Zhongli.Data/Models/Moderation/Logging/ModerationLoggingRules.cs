using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Logging;

public class ModerationLoggingRules
{
    public Guid Id { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public LogReprimandType SilentReprimands { get; set; } = LogReprimandType.None;

    public virtual ModerationLogChannelConfig ModeratorLog { get; set; } = new();

    public virtual ModerationLogChannelConfig PublicLog { get; set; } = new();

    public virtual ModerationLogConfig CommandLog { get; set; } = new();

    public virtual ModerationLogConfig UserLog { get; set; } = new();

    public ulong GuildId { get; set; }
}