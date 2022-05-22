using System;
using Discord.Interactions;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig.ModerationLogOptions;

namespace HuTao.Data.Models.Moderation.Logging;

public class ModerationLogConfig
{
    [Flags]
    public enum ModerationLogOptions
    {
        [Hide] None = 0,
        ShowReprimandId = 1 << 0,
        ShowDetails = 1 << 1,
        ShowReason = 1 << 2,
        ShowModerator = 1 << 3,
        ShowUser = 1 << 4,
        ShowAvatarThumbnail = 1 << 5,
        ShowActive = 1 << 6,
        ShowTotal = 1 << 7,
        ShowTrigger = 1 << 8,
        ShowCategory = 1 << 9,

        All = ShowReprimandId | ShowDetails | ShowReason | ShowModerator | ShowUser
            | ShowAvatarThumbnail | ShowActive | ShowTotal | ShowTrigger | ShowCategory
    }

    private const ModerationLogOptions UserOptions = All & ~ShowReprimandId & ~ShowModerator & ~ShowTrigger;

    public Guid Id { get; set; }

    public LogReprimandStatus? LogReprimandStatus { get; set; }

    public LogReprimandType? LogReprimands { get; set; }

    public LogReprimandType? ShowAppealOnReprimands { get; set; }

    public static ModerationLogConfig DefaultCommandLogConfig { get; } = new()
    {
        LogReprimandStatus     = Logging.LogReprimandStatus.All,
        LogReprimands          = LogReprimandType.All,
        ShowAppealOnReprimands = LogReprimandType.None,
        Options                = All
    };

    public static ModerationLogConfig DefaultUserLogConfig { get; } = new()
    {
        LogReprimandStatus     = Logging.LogReprimandStatus.All,
        LogReprimands          = LogReprimandType.All & ~LogReprimandType.Note,
        ShowAppealOnReprimands = LogReprimandType.Ban,
        Options                = UserOptions
    };

    public ModerationLogOptions? Options { get; set; }

    public string? AppealMessage { get; set; }
}