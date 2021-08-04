using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Utilities;

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
            => reprimand.Status is not ReprimandStatus.Hidden or ReprimandStatus.Deleted;

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

        public static bool IsActive(this IExpirable expirable)
            => expirable.EndedAt is null || expirable.ExpireAt >= DateTimeOffset.Now;

        public static async Task<AutoModerationRules> GetAutoModerationRulesAsync(this ReprimandDetails details, ZhongliContext db,
            CancellationToken cancellationToken)
        {
            var guild = await details.GetGuildAsync(db, cancellationToken);
            return guild.AutoModerationRules;
        }

        public static Task<GuildEntity> GetGuildAsync(this ReprimandDetails details, ZhongliContext db,
            CancellationToken cancellationToken)
            => db.Guilds.TrackGuildAsync(details.User.Guild, cancellationToken);
    }
}