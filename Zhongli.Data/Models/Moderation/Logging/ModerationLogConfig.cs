using System;

namespace Zhongli.Data.Models.Moderation.Logging;

public class ModerationLogConfig
{
    [Flags]
    public enum ModerationLogOptions
    {
        None = 0,
        ShowReprimandId = 1 << 0,
        ShowDetails = 1 << 1,
        ShowReason = 1 << 2,
        ShowModerator = 1 << 3,
        ShowUser = 1 << 4,
        ShowAvatarThumbnail = 1 << 5,
        ShowActive = 1 << 6,
        ShowTotal = 1 << 7,
        ShowTrigger = 1 << 8
    }

    public Guid Id { get; set; }

    public LogReprimandStatus LogReprimandStatus { get; set; } = LogReprimandStatus.All;

    public LogReprimandType LogReprimands { get; set; } = LogReprimandType.None;

    public LogReprimandType ShowAppealOnReprimands { get; set; } = LogReprimandType.None;

    public ModerationLogOptions Options { get; set; } = ModerationLogOptions.None;

    public string? AppealMessage { get; set; }
}