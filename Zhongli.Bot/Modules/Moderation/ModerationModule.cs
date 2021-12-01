using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : InteractiveBase
    {
        private readonly CommandErrorHandler _error;
        private readonly ModerationLoggingService _logging;
        private readonly ModerationService _moderation;
        private readonly ZhongliContext _db;

        public ModerationModule(
            CommandErrorHandler error,
            ZhongliContext db,
            ModerationLoggingService logging,
            ModerationService moderation)
        {
            _error = error;
            _db    = db;

            _logging    = logging;
            _moderation = moderation;
        }

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IUser user, uint deleteDays = 0, TimeSpan? length = null,
            [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.TryBanAsync(deleteDays, length, details);

            if (result is null)
                await _error.AssociateError(Context.Message, "Failed to ban user.");
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("ban")]
        [HiddenFromHelp]
        [Summary("Ban a user permanently from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public Task BanAsync(IUser user, [Remainder] string? reason = null)
            => BanAsync(user, 0, null, reason);

        [Command("ban")]
        [HiddenFromHelp]
        [Summary("Ban a user permanently from the current guild, and delete messages.")]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public Task BanAsync(IUser user, uint deleteDays = 0, [Remainder] string? reason = null)
            => BanAsync(user, deleteDays, null, reason);

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.TryKickAsync(details);

            if (result is null)
                await _error.AssociateError(Context.Message, "Failed to kick user.");
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.TryMuteAsync(length, details);

            if (result is null)
            {
                await _error.AssociateError(Context.Message, "Failed to mute user. " +
                    "Either the user is already muted or there is no mute role configured. " +
                    "Configure the mute role by running the 'configure mute' command.");
            }
            else
                await ReplyReprimandAsync(result, details);
        }

        [Command("note")]
        [Summary("Add a note to a user. Notes are always silent.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoteAsync(IGuildUser user, [Remainder] string? note = null)
        {
            var details = await GetDetailsAsync(user, note);
            await _moderation.NoteAsync(details);

            await Context.Message.DeleteAsync();
        }

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoticeAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.NoticeAsync(details);
            await ReplyReprimandAsync(result, details);
        }

        [Command("say")]
        [Summary("Make the bot send a message to the specified channel")]
        [RequireAuthorization(AuthorizationScope.Helper)]
        public async Task SayAsync(ITextChannel? channel, [Remainder] string message)
        {
            channel ??= (ITextChannel) Context.Channel;
            await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            await channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
        }

        [Command("say")]
        [HiddenFromHelp]
        [Summary("Make the bot send a message to the specified channel")]
        [RequireAuthorization(AuthorizationScope.Helper)]
        public async Task SayAsync([Remainder] string message)
            => SayAsync(null, message);

        [Command("slowmode")]
        [Summary("Set a slowmode in the channel.")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireAuthorization(AuthorizationScope.Helper)]
        public async Task SlowmodeAsync(TimeSpan? length = null, ITextChannel? channel = null)
        {
            if (length is null && channel is null)
            {
                var channels = Context.Guild.Channels.OfType<ITextChannel>()
                    .Where(c => c is not INewsChannel)
                    .Where(c => c.SlowModeInterval is not 0)
                    .OrderBy(c => c.Position);

                var embed = new EmbedBuilder()
                    .WithTitle("List of channels with slowmode active")
                    .AddItemsIntoFields("Channels", channels,
                        c => $"{c.Mention} => {c.SlowModeInterval.Seconds().Humanize()}")
                    .WithColor(Color.Green)
                    .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

                await ReplyAsync(embed: embed.Build());
            }
            else
            {
                length  ??= TimeSpan.Zero;
                channel ??= (ITextChannel) Context.Channel;
                var seconds = (int) length.Value.TotalSeconds;
                await channel.ModifyAsync(c => c.SlowModeInterval = seconds);

                if (seconds is 0)
                    await ReplyAsync($"Slowmode disabled for {channel.Mention}");
                else
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Slowmode enabled")
                        .AddField("Channel", channel.Mention, true)
                        .AddField("Delay", length.Value.Humanize(3), true)
                        .WithColor(Color.Green)
                        .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

                    await ReplyAsync(embed: embed.Build());
                }
            }
        }

        [Command("slowmode")]
        [HiddenFromHelp]
        [Summary("Set a slowmode in the channel.")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireAuthorization(AuthorizationScope.Helper)]
        public Task SlowmodeAsync(ITextChannel? channel = null, TimeSpan? length = null)
            => SlowmodeAsync(length, channel);

        [Command("unban")]
        [Summary("Unban a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task UnbanAsync(IUser user, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.TryUnbanAsync(details);

            if (result is not null)
                await ReplyUpdatedReprimandAsync(result, details);
            else
                await _error.AssociateError(Context.Message, "This user has no ban logs. Forced unban.");
        }

        [Command("unmute")]
        [Summary("Unmute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task UnmuteAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.TryUnmuteAsync(details);

            if (result is not null)
                await ReplyUpdatedReprimandAsync(result, details);
            else
                await _error.AssociateError(Context.Message, "Unmute failed.");
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var details = await GetDetailsAsync(user, reason);
            var result = await _moderation.WarnAsync(amount, details);
            await ReplyReprimandAsync(result, details);
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild once.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public Task WarnAsync(IGuildUser user, [Remainder] string? reason = null)
            => WarnAsync(user, 1, reason);

        [Command("mute list")]
        [Summary("View active mutes on the current guild.")]
        [RequireAuthorization(AuthorizationScope.Moderator)]
        public async Task MuteListAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            var history = guild.ReprimandHistory
                .Where(r => r.Status is not ReprimandStatus.Deleted && r.Status is not ReprimandStatus.Expired && r.Status is not ReprimandStatus.Hidden)
                .OfType(InfractionType.Mute);

            var reprimands = new EmbedBuilder().Fields;

            var pages = history
                .OrderByDescending(r => r.Action?.Date)
                .Select(r => CreateEmbed(r));

            var message = new PaginatedMessage
            {
                Title = "Currently Active Mutes",
                Pages = reprimands.Concat(pages),
                Options = new PaginatedAppearanceOptions
                {
                    FieldsPerPage = 10
                }
            };
            await _db.SaveChangesAsync();
            await PagedReplyAsync(message);
        }

        private async Task ReplyReprimandAsync(ReprimandResult result, ReprimandDetails details)
        {
            var guild = await result.Primary.GetGuildAsync(_db);
            if (!guild.ModerationRules.Options.HasFlag(ReprimandOptions.Silent))
            {
                var embed = await _logging.CreateEmbedAsync(result, details);
                await ReplyAsync(embed: embed.Build());
            }
            else
                await Context.Message.DeleteAsync();
        }

        private async Task ReplyUpdatedReprimandAsync(Reprimand reprimand, ReprimandDetails details)
        {
            var guild = await reprimand.GetGuildAsync(_db);
            if (!guild.ModerationRules.Options.HasFlag(ReprimandOptions.Silent))
            {
                var embed = await _logging.UpdatedEmbedAsync(reprimand, details);
                await ReplyAsync(embed: embed.Build());
            }
            else
                await Context.Message.DeleteAsync();
        }

        private async Task<ReprimandDetails> GetDetailsAsync(IUser user, string? reason)
        {
            var details = new ReprimandDetails(user, (IGuildUser) Context.User, reason);

            await _db.Users.TrackUserAsync(details);
            await _db.SaveChangesAsync();

            return details;
        }

        private static EmbedFieldBuilder CreateEmbed(Reprimand r) => new EmbedFieldBuilder()
            .WithName(r.GetTitle())
            .WithValue(r.GetReprimandExpiration());
    }
}