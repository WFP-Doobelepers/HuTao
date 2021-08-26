using Zhongli.Data.Models.Discord;

namespace Zhongli.Data.Models.Criteria
{
    public class UserCriterion : Criterion, IUserEntity
    {
        protected UserCriterion() { }

        public UserCriterion(ulong userId) { UserId = userId; }

        public ulong UserId { get; set; }

        public override string ToString() => this.MentionUser();
    }
}