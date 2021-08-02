using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules
{
    public class UserModule : InteractiveBase
    {
        public enum InfractionType
        {
            All,
            Ban,
            Kick,
            Mute,
            Note,
            Notice,
            Warning
        }

        private readonly AuthorizationService _auth;
        private readonly ZhongliContext _db;

        public UserModule(AuthorizationService auth, ZhongliContext db)
        {
            _auth = auth;
            _db   = db;
        }

        [Command("user")]
        [Summary("Views the information of a user")]
        public async Task UserAsync(IUser? user = null)
        {
            user ??= Context.User;

            var userEntity = await _db.Users.FindAsync(user.Id, Context.Guild.Id);
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var guildUser = user as SocketGuildUser;

            var embed = new EmbedBuilder()
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
                .AddField($"Created {user.CreatedAt.Humanize()}", user.CreatedAt)
                .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            if (guildUser is not null)
            {
                embed
                    .AddField($"Joined {guildUser.JoinedAt.Humanize()}", guildUser.JoinedAt)
                    .AddField($"Roles ({guildUser.Roles.Count})",
                        string.Join(" ", guildUser.Roles.Select(r => r.Mention)));
            }

            if (await _auth.IsAuthorized(Context, AuthorizationScope.Moderator) && userEntity is not null)
            {
                embed
                    .AddReprimands(userEntity)
                    .AddField("Muted", guildUser?.HasRole(guild.MuteRoleId ?? 0), true);
            }

            await ReplyAsync(embed: embed.Build());
        }

        [Priority(-1)]
        [HiddenFromHelp]
        [Command("user")]
        public async Task UserAsync(ulong userId)
        {
            var user = Context.Client.GetUser(userId);
            await UserAsync(user);
        }

        [Command("history")]
        [Alias("infraction", "infractions", "reprimand", "reprimands")]
        [Summary("View a specific history of a user's infractions.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
        public async Task InfractionsAsync(
            [Summary("The user to show the infractions of.")]
            IUser? user = null,
            [Summary("Leave empty to show everything.")]
            InfractionType type = InfractionType.All)
        {
            user ??= Context.User;
            var userEntity = _db.Users.FirstOrDefault(u => u.Id == user.Id && u.GuildId == Context.Guild.Id);
            if (userEntity is null)
                return;

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var history = guild.ReprimandHistory.Where(u => u.UserId == user.Id);
            history = type switch
            {
                InfractionType.Ban     => history.OfType<Ban>(),
                InfractionType.Kick    => history.OfType<Kick>(),
                InfractionType.Mute    => history.OfType<Mute>(),
                InfractionType.Note    => history.OfType<Note>(),
                InfractionType.Notice  => history.OfType<Notice>(),
                InfractionType.Warning => history.OfType<Warning>(),
                InfractionType.All     => history,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                    "Invalid Infraction type.")
            };

            var reprimands = new EmbedBuilder().AddReprimands(userEntity).Fields;
            var pages = history
                .OrderByDescending(r => r.Action.Date)
                .Select(r => CreateEmbed(user, r));

            var message = new PaginatedMessage
            {
                Pages  = reprimands.Concat(pages),
                Author = new EmbedAuthorBuilder().WithName($"{user}"),
                Options = new PaginatedAppearanceOptions
                {
                    DisplayInformationIcon = false
                }
            };

            await PagedReplyAsync(message);
        }

        private static EmbedFieldBuilder CreateEmbed(IUser user, ReprimandAction r)
        {
            var moderator = r.Action.Moderator;
            var content = new StringBuilder()
                .AppendLine($"▌{ModerationLoggingService.GetMessage(r, user)}")
                .AppendLine(GetReason(r.Action))
                .AppendLine(GetModerator(moderator))
                .AppendLine(GetDate(r.Action.Date))
                .AppendLine(GetStatus(r.Status));

            if (r.Status is not ReprimandStatus.Added && r.ModifiedAction is not null)
            {
                content
                    .AppendLine($"▌▌{r.Status.Humanize()} by {GetModifiedInfo(r.ModifiedAction)}")
                    .AppendLine($"▌{GetReason(r.ModifiedAction)}");
            }

            return new EmbedFieldBuilder()
                .WithName(ModerationLoggingService.GetTitle(r))
                .WithValue(content.ToString());
        }

        private static string GetReason(ModerationAction action)
            => $"▌Reason: {Format.Bold(action.Reason ?? "None")}";

        private static string GetModerator(GuildUserEntity user)
            => $"▌Moderator: {GetUser(user)} ({user.Id})";

        private static string GetDate(DateTimeOffset date)
            => $"▌Date: {Format.Bold(date.Humanize())} ({date.ToUniversalTime()})";

        private static string GetStatus(ReprimandStatus status)
            => $"▌Status: {Format.Bold(status.Humanize())}";

        private static string GetUser(GuildUserEntity user)
            => Format.Bold($"{user.Username}#{user.DiscriminatorValue}");

        private static string GetModifiedInfo(ModerationAction action)
        {
            var modified = action.Moderator;
            return $"{GetUser(modified)} {modified.Id} ({action.Date.Humanize()})";
        }

        public async Task HideReprimandAsync(Guid id)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

            var reprimand = guild.ReprimandHistory.FirstOrDefault(r => r.Id == id);
            if (reprimand is null)
                return;

            reprimand.Status = ReprimandStatus.Hidden;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }
}