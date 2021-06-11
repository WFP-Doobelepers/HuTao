using System;

namespace Zhongli.Data.Models.Moderation
{
    public class AntiSpamRules
    {
        public Guid Id { get; set; }

        public int? DuplicateTolerance { get; set; }

        public TimeSpan? DuplicateMessageTime { get; set; }

        public TimeSpan? MessageSpamTime { get; set; }

        public uint? EmojiLimit { get; set; }

        public uint? MessageLimit { get; set; }

        public uint? NewlineLimit { get; set; }
    }
}