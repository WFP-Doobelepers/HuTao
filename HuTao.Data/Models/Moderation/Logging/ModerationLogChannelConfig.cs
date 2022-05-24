using HuTao.Data.Models.Discord;
using static HuTao.Data.Models.Moderation.Logging.ModerationLogConfig.ModerationLogOptions;

namespace HuTao.Data.Models.Moderation.Logging;

public class ModerationLogChannelConfig : ModerationLogConfig, IChannelEntity
{
    private const ModerationLogOptions PublicOptions = All
        & ~ShowReprimandId & ~ShowModerator
        & ~ShowActive & ~ShowTotal
        & ~ShowTrigger & ~ShowCategory;

    public static ModerationLogChannelConfig DefaultModeratorLogConfig { get; } = new()
    {
        LogReprimandStatus     = Logging.LogReprimandStatus.All,
        LogReprimands          = LogReprimandType.All,
        ShowAppealOnReprimands = LogReprimandType.None,
        Options                = All
    };

    public static ModerationLogChannelConfig DefaultPublicLogConfig { get; } = new()
    {
        LogReprimandStatus     = Logging.LogReprimandStatus.All,
        LogReprimands          = LogReprimandType.All,
        ShowAppealOnReprimands = LogReprimandType.None,
        Options                = PublicOptions
    };

    public ulong ChannelId { get; set; }
}