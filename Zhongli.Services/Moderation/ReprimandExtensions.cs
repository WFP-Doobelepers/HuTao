using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Services.Moderation
{
    public static class ReprimandExtensions
    {
        public static async ValueTask<GuildEntity> GetGuildAsync(this ReprimandAction reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.Guild ??
                await db.FindAsync<GuildEntity>(new object[] { reprimand.GuildId }, cancellationToken);
        }

        public static int HistoryCount<T>(this GuildUserEntity user) where T : ReprimandAction
            => Reprimands<T>(user).Count();

        private static IEnumerable<T> Reprimands<T>(this GuildUserEntity user) => user.Guild.ReprimandHistory
            .Where(r => r.UserId == user.Id)
            .OfType<T>();

        public static int ReprimandCount<T>(this GuildUserEntity user) where T : ICountable
            => (int) Reprimands<T>(user).Sum(w => w.Amount);

        public static async ValueTask<GuildUserEntity> GetUserAsync(this ReprimandAction reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.User ??
                await db.FindAsync<GuildUserEntity>(new object[] { reprimand.UserId, reprimand.GuildId },
                    cancellationToken);
        }
    }
}