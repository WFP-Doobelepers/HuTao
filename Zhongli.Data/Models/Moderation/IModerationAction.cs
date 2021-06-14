using System;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation
{
    public interface IModerationAction
    {
        Guid Id { get; set; }

        DateTimeOffset Date { get; set; }

        GuildEntity Guild { get; set; }

        GuildUserEntity Moderator { get; set; }

        GuildUserEntity User { get; set; }

        string? Reason { get; set; }
    }
}