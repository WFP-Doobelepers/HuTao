namespace HuTao.Data.Models.Discord;

public interface IRoleEntity : IGuildEntity
{
    ulong RoleId { get; set; }
}