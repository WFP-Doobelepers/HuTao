namespace Zhongli.Data.Models.Authorization
{
    public class ChannelAuthorization : AuthorizationRule
    {
        protected ChannelAuthorization() { }

        public ChannelAuthorization(ulong channelId, bool isCategory)
        {
            ChannelId  = channelId;
            IsCategory = isCategory;
        }

        public ulong ChannelId { get; set; }

        public bool IsCategory { get; set; }
    }
}