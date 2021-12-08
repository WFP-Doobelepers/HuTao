using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Zhongli.Data.Config;

namespace Zhongli.Data;

public class ZhongliContextFactory : IDesignTimeDbContextFactory<ZhongliContext>
{
    public ZhongliContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ZhongliContext>()
            .UseNpgsql(ZhongliConfig.Configuration.ZhongliContext);

        return new ZhongliContext(optionsBuilder.Options);
    }
}