using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers
{
    public class NoticeTrigger : WarningTrigger
    {
        protected NoticeTrigger() { }

        public NoticeTrigger(uint amount) { Amount = amount; }

        public override bool IsTriggered(GuildUserEntity user) => user.NoticeCount == Amount;
    }
}