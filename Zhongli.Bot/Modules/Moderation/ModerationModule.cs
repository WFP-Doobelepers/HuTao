using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        private readonly ModerationService _moderationService;

        public ModerationModule(ModerationService moderationService) { _moderationService = moderationService; }

        private ReprimandDetails GetDetails(IGuildUser user, string? reason)
            => new(user, (IGuildUser) Context.User, ModerationSource.Command, reason);

        [Command("ban")]
        [Summary("Ban a user from the current guild.")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireAuthorization(AuthorizationScope.Ban)]
        public async Task BanAsync(IGuildUser user, uint deleteDays = 1, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryBanAsync(deleteDays, GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been banned.");
            else
                await ReplyAsync("Ban failed.");
        }

        [Command("kick")]
        [Summary("Kick a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Kick)]
        public async Task KickAsync(IGuildUser user, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryKickAsync(GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been kicked.");
            else
                await ReplyAsync("Kick failed.");
        }

        [Command("mute")]
        [Summary("Mute a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Mute)]
        public async Task MuteAsync(IGuildUser user, TimeSpan? length = null, [Remainder] string? reason = null)
        {
            if (await _moderationService.TryMuteAsync(length, GetDetails(user, reason)) is not null)
                await ReplyAsync($"{user} has been muted.");
            else
                await ReplyAsync("Mute failed.");
        }

        [Command("warn")]
        [Summary("Warn a user from the current guild.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task WarnAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var action = await _moderationService.WarnAsync(amount, GetDetails(user, reason));
            var embed = CreateEmbed(user, action)
                .WithTitle("Warning")
                .WithDescription($"{user.Mention} was given {amount} warnings.")
                .AddField("Total", action.User.ReprimandCount<Warning>(), true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("notice")]
        [Summary("Add a notice to a user. This counts as a minor warning.")]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoticeAsync(IGuildUser user, uint amount = 1, [Remainder] string? reason = null)
        {
            var action = await _moderationService.NoticeAsync(GetDetails(user, reason));
            var embed = CreateEmbed(user, action)
                .WithTitle("Notice")
                .WithDescription($"{user.Mention} was given {amount} notices.")
                .AddField("Total", action.User.HistoryCount<Notice>(), true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("note")]
        [Summary("Add a note to a user. This does nothing.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireAuthorization(AuthorizationScope.Warning)]
        public async Task NoteAsync(IGuildUser user, [Remainder] string? note = null)
        {
            await _moderationService.NoteAsync(GetDetails(user, note));

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        private EmbedBuilder CreateEmbed(IUser user, ReprimandAction action)
        {
            var embed = new EmbedBuilder()
                .WithUserAsAuthor(Context.User, AuthorOptions.IncludeId | AuthorOptions.UseFooter)
                .WithUserAsAuthor(user, AuthorOptions.Requested | AuthorOptions.UseFooter)
                .WithCurrentTimestamp();

            AddReprimands(embed, action);

            return embed;
        }

        private static void AddReprimands(EmbedBuilder embed, ReprimandAction action)
        {
            if (action.Source != ModerationSource.Auto) return;

            switch (action)
            {
                case Ban:
                    embed.AddField("Reprimands", "Additionally got banned.");
                    break;
                case Kick:
                    embed.AddField("Reprimands", "Additionally got kicked.");
                    break;
                case Mute mute:
                    embed.AddField("Reprimands", $"Additionally got muted for {mute.Length}.");
                    break;
                case Warning:
                    embed.AddField("Reprimands", "Additionally got warned");
                    break;
            }
        }
    }
}