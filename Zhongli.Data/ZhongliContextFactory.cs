using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Zhongli.Data
{
    public class ZhongliContextFactory : IDesignTimeDbContextFactory<ZhongliContext>
    {
        public ZhongliContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ZhongliContextFactory>()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ZhongliContext>()
                .UseNpgsql(configuration.GetConnectionString(nameof(ZhongliContext)));

            return new ZhongliContext(optionsBuilder.Options);
        }
    }
}