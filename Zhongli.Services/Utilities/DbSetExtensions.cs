using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Zhongli.Services.Utilities
{
    public static class DbSetExtensions
    {
        public static ValueTask<T> FindByIdAsync<T>(this DbSet<T> dbSet, object key, CancellationToken cancellationToken)
            where T : class => dbSet.FindAsync(new[] { key }, cancellationToken);
    }
}