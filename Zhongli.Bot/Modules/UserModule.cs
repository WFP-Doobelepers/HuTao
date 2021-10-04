using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
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
        private readonly AuthorizationService _auth;
        private readonly ZhongliContext _db;

        public UserModule(AuthorizationService auth, ZhongliContext db)
        {
            _auth = auth;
            _db   = db;
        }

        [Command("avatar")]
        [Alias("av")]
        [Summary("Get the avatar of the user. Leave empty to view your own avatar.")]
        public async Task AvatarAsync(
            [Summary("The mention, username or ID of the user.")]
            IUser? user = null)
        {
            user ??= Context.User;

            var embed = new EmbedBuilder()
                .WithUserAsAuthor(user, AuthorOptions.IncludeId)
                .WithImageUrl(user.GetAvatarUrl(size: 2048))
                .WithColor(Color.Green)
                .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("history")]
        [Alias("infraction", "infractions", "reprimand", "reprimands", "warnlist")]
        [Summary("View a specific history of a user's infractions.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
        public async Task InfractionsAsync(
            [Summary("The user to show the infractions of.")]
            IUser? user = null,
            [Summary("Leave empty to show warnings.")]
            InfractionType type = InfractionType.Warning)
        {
            user ??= Context.User;
            var userEntity = _db.Users.FirstOrDefault(u => u.Id == user.Id && u.GuildId == Context.Guild.Id);
            if (userEntity is null)
                return;

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var history = guild.ReprimandHistory
                .Where(u => u.UserId == user.Id)
                .OfType(type);

            var reprimands = new EmbedBuilder().AddReprimands(userEntity)
                .AddField("Reprimands", "Active/Total", true).Fields;
            var pages = history
                .OrderByDescending(r => r.Action?.Date)
                .Select(r => CreateEmbed(user, r));

            var message = new PaginatedMessage
            {
                Pages  = reprimands.Concat(pages),
                Author = new EmbedAuthorBuilder().WithName($"{user}"),
                Options = new PaginatedAppearanceOptions
                {
                    DisplayInformationIcon = false,
                    FieldsPerPage          = 8
                }
            };

            await PagedReplyAsync(message);
        }

        [Command("user")]
        [Alias("whois")]
        [Summary("Views the information of a user. Leave blank to view self.")]
        public async Task UserAsync(
            [Summary("The mention, username or ID of the user.")]
            IUser? user = null)
        {
            user ??= Context.User;

            var isAuthorized
                = await _auth.IsAuthorizedAsync(Context, AuthorizationScope.All | AuthorizationScope.Moderator);
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
                    .WithColor(guildUser.Roles.OrderByDescending(r => r.Position).Select(x => x.Color).FirstOrDefault())
                    .AddField($"Roles ({guildUser.Roles.Count})",
                        string.Join(" ", guildUser.Roles.OrderByDescending(r => r.Position).Select(r => r.Mention)));

                if (isAuthorized && guild.ModerationRules.MuteRoleId is not null)
                {
                    var isMuted = guildUser.HasRole(guild.ModerationRules.MuteRoleId.Value);
                    embed.AddField("Muted", isMuted, true);
                }
            }

            if (isAuthorized && userEntity is not null) embed.AddReprimands(userEntity);

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

        private static EmbedFieldBuilder CreateEmbed(IUser user, Reprimand r) => new EmbedFieldBuilder()
            .WithName(r.GetTitle())
            .WithValue(r.GetReprimandDetails().ToString());
    }
}