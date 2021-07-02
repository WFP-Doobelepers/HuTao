namespace Zhongli.Data.Models.Authorization
{
    public class ChannelAuthorization : AuthorizationRule
    {
        public ulong ChannelId { get; set; }
    }
}