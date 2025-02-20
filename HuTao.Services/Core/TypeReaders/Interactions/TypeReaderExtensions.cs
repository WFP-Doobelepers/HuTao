using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public static class TypeReaderExtensions
{
    public static void AddUserTypeReader<TUser>(
        this InteractionService commands,
        CacheMode cacheMode = CacheMode.AllowDownload) where TUser : class, IUser
        => commands.AddTypeReader<TUser>(new UserTypeReader<TUser>(cacheMode));
}