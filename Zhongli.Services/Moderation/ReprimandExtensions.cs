using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public static class ReprimandExtensions
    {
        public static bool IsActive(this IExpirable expirable)
            => expirable.EndedAt is null || expirable.ExpireAt >= DateTimeOffset.Now;

        public static bool IsTriggered(this ITrigger trigger, uint amount)
        {
            return trigger.Mode switch
            {
                TriggerMode.Default     => amount == trigger.Amount,
                TriggerMode.Retroactive => amount >= trigger.Amount,
                TriggerMode.Multiple    => amount % trigger.Amount is 0
            };
        }

        public static EmbedBuilder AddReprimands(this EmbedBuilder embed, GuildUserEntity user) => embed
            .AddField("Warnings", $"{user.WarningCount()}/{user.WarningCount(true)}", true)
            .AddReprimand<Notice>(user)
            .AddReprimand<Ban>(user)
            .AddReprimand<Kick>(user)
            .AddReprimand<Note>(user);

        public static Task<GuildEntity> GetGuildAsync(this ReprimandDetails details, ZhongliContext db,
            CancellationToken cancellationToken)
            => db.Guilds.TrackGuildAsync(details.User.Guild, cancellationToken);

        public static async Task<uint> CountAsync<T>(this T reprimand, DbContext db,
            CancellationToken cancellationToken = default) where T : ReprimandAction
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);

            return user.HistoryCount<T>();
        }

        public static async Task<uint> CountAsync(this Warning reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);

            return user.WarningCount();
        }

        public static uint HistoryCount<T>(this GuildUserEntity user, bool showHidden = false) where T : ReprimandAction
            => (uint) Reprimands<T>(user, showHidden).LongCount();

        public static uint WarningCount(this GuildUserEntity user, bool showHidden = false)
            => (uint) Reprimands<Warning>(user, showHidden).Sum(w => w.Count);

        public static async ValueTask<GuildEntity> GetGuildAsync(this ReprimandAction reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.Guild ??
                await db.FindAsync<GuildEntity>(new object[] { reprimand.GuildId }, cancellationToken);
        }

        public static async ValueTask<GuildUserEntity> GetUserAsync(this ReprimandAction reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.User ??
                await db.FindAsync<GuildUserEntity>(new object[] { reprimand.UserId, reprimand.GuildId },
                    cancellationToken);
        }

        private static bool IsCounted(ReprimandAction reprimand)
            => reprimand.Status is ReprimandStatus.Added or ReprimandStatus.Updated;

        private static EmbedBuilder AddReprimand<T>(this EmbedBuilder embed, GuildUserEntity user)
            where T : ReprimandAction => embed
            .AddField(typeof(T).Name.Pluralize(), $"{user.HistoryCount<T>()}/{user.HistoryCount<T>(true)}", true);

        private static IEnumerable<T> Reprimands<T>(this GuildUserEntity user, bool countHidden = false)
            where T : ReprimandAction
        {
            var reprimands = user.Guild.ReprimandHistory
                .Where(r => r.UserId == user.Id
                    && r.Status is not ReprimandStatus.Deleted)
                .OfType<T>();

            return countHidden
                ? reprimands
                : reprimands.Where(IsCounted);
        }
    }
}