using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Zhongli.Data.Config;

namespace Zhongli.Data
{
    public class ZhongliContextFactory : IDesignTimeDbContextFactory<ZhongliContext>
    {
        public ZhongliContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ZhongliContextFactory>()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ZhongliContext>();

#if DEBUG
            optionsBuilder.UseNpgsql(configuration.GetSection(nameof(ZhongliConfig.Debug))[nameof(BotConfig.ZhongliContext)]);
#else
            optionsBuilder.UseNpgsql(configuration.GetSection(nameof(ZhongliConfig.Release))[nameof(BotConfig.ZhongliContext)]));
#endif

            return new ZhongliContext(optionsBuilder.Options);
        }
    }
}