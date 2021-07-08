using Zhongli.Data.Models.Criteria;

namespace Zhongli.Data.Models.Discord
{
    public interface IPermissionEntity
    {
        GuildPermission Permission { get; set; }
    }
}