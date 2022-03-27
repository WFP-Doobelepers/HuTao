using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Logging;

public class UserJoinLog : ILog, IGuildUserEntity
{
    public UserJoinLog(GuildUserEntity user, DateTimeOffset userJoinDate)
    {
        LogDate       = DateTimeOffset.UtcNow;
        User          = user;
        UserId        = user.Id;
        GuildId       = user.GuildId;
        FirstJoinDate = (DateTimeOffset) user.JoinedAt;
        JoinDate      = userJoinDate;
    }

    public DateTimeOffset LogDate { get; set; }
    public GuildUserEntity User { get; set; }
    public DateTimeOffset FirstJoinDate { get; set; }
    public DateTimeOffset JoinDate { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
}