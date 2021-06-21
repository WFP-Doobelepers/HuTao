using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Zhongli.Data.Models.Discord;

namespace Zhongli.Services.Utilities
{
    public static class ProxyExtensions
    {
        private static EntityEntry<TEntity> CreateAndAddProxyInternal<TEntity>(this DbSet<TEntity> set,
            params object[] constructorArguments) where TEntity : class
            => set.Add(set.CreateProxy(constructorArguments));

        public static GuildUserEntity CreateAndAddProxy(this DbSet<GuildUserEntity> set, IGuildUser user) =>
            set.CreateAndAddProxyInternal(user).Entity;
    }
}