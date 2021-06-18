using System;
using System.ComponentModel.DataAnnotations.Schema;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public abstract class ReprimandActionBase
    {
        protected ReprimandActionBase() { }

        protected ReprimandActionBase(ReprimandDetails details)
        {
            Date = DateTimeOffset.UtcNow;

            GuildId     = details.GuildId;
            ModeratorId = details.ModeratorId;
            UserId      = details.UserId;

            Type   = details.Type;
            Reason = details.Reason;
        }

        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; }

        [ForeignKey(nameof(GuildId))] public virtual GuildEntity Guild { get; set; }

        public virtual GuildUserEntity Moderator { get; set; }

        public virtual GuildUserEntity User { get; set; }

        public ModerationActionType Type { get; set; }

        public string? Reason { get; set; }

        public ulong GuildId { get; set; }

        public ulong ModeratorId { get; set; }

        public ulong UserId { get; set; }
    }

    public enum ModerationActionType
    {
        Added,
        Removed
    }
}