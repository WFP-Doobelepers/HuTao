using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord;
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

        public static EmbedBuilder AddReprimands(this EmbedBuilder embed, GuildUserEntity user)
            => embed
                .AddField("Warnings", user.WarningCount(), true)
                .AddField("Notices", user.HistoryCount<Notice>(), true)
                .AddField("Bans", user.HistoryCount<Ban>(), true)
                .AddField("Kicks", user.HistoryCount<Kick>(), true)
                .AddField("Notes", user.HistoryCount<Note>(), true);

        public static int HistoryCount<T>(this GuildUserEntity user) where T : ReprimandAction
            => Reprimands<T>(user).Count(IsCounted);

        private static IEnumerable<T> Reprimands<T>(this GuildUserEntity user) => user.Guild.ReprimandHistory
            .Where(r => r.UserId == user.Id)
            .OfType<T>();

        private static bool IsCounted(ReprimandAction reprimand)
            => reprimand.Status is ReprimandStatus.Added or ReprimandStatus.Updated;

        public static int WarningCount(this GuildUserEntity user)
            => (int) Reprimands<Warning>(user).Where(IsCounted).Sum(w => w.Amount);

        public static async ValueTask<GuildUserEntity> GetUserAsync(this ReprimandAction reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.User ??
                await db.FindAsync<GuildUserEntity>(new object[] { reprimand.UserId, reprimand.GuildId },
                    cancellationToken);
        }

        public static async Task<int> CountAsync<T>(this T reprimand, DbContext db,
            CancellationToken cancellationToken = default) where T : ReprimandAction
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);

            return user.HistoryCount<T>();
        }

        public static async Task<int> CountAsync(this Warning reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);

            return user.WarningCount();
        }
    }
}