using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.VoiceChat;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("configure")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ConfigureModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public ConfigureModule(ZhongliContext db) { _db = db; }

        [Command("mute")]
        [Summary("Configures the Mute role.")]
        public async Task ConfigureMuteAsync(
            [Summary("Optionally provide a mention, ID, or name of an existing role.")]
            IRole? role)
        {
            await ModerationService.ConfigureMuteRoleAsync(Context.Guild, role);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("autoPardon notice")]
        [Summary("Set the time for when a notice is automatically pardoned. This will not affect old cases.")]
        public async Task ConfigureAutoPardonNoticeAsync(
            [Summary("Leave empty to disable auto pardon of notices.")]
            TimeSpan? length = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.NoticeAutoPardonLength = length;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("autoPardon warning")]
        [Summary("Set the time for when a warning is automatically pardoned. This will not affect old cases.")]
        public async Task ConfigureAutoPardonWarningAsync(
            [Summary("Leave empty to disable auto pardon of notices.")]
            TimeSpan? length = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.WarningAutoPardonLength = length;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("appeal message")]
        [Summary("Set the appeal message when someone is reprimanded.")]
        public async Task ConfigureAppealMessageAsync(
            [Summary("Leave empty to disable the appeal message.")] [Remainder]
            string? message = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.ModerationRules.ReprimandAppealMessage = message;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("voice")]
        [Summary("Configures the Voice Chat settings. Leave the categories empty to use the same one the hub uses.")]
        public async Task ConfigureVoiceChatAsync(
            [Summary(
                "Mention, ID, or name of the hub voice channel that the user can join to create a new voice chat.")]
            IVoiceChannel hubVoiceChannel, VoiceChatOptions? options = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);

            if (hubVoiceChannel.CategoryId is null)
                return;

            if (guild.VoiceChatRules is not null)
                _db.Remove(guild.VoiceChatRules);

            guild.VoiceChatRules = new VoiceChatRules
            {
                GuildId                = guild.Id,
                HubVoiceChannelId      = hubVoiceChannel.Id,
                VoiceChannelCategoryId = options?.VoiceChannelCategory?.Id ?? hubVoiceChannel.CategoryId.Value,
                VoiceChatCategoryId    = options?.VoiceChatCategory?.Id ?? hubVoiceChannel.CategoryId.Value,
                PurgeEmpty             = options?.PurgeEmpty ?? true,
                ShowJoinLeave          = options?.ShowJoinLeave ?? true
            };

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [NamedArgumentType]
        public class VoiceChatOptions
        {
            [HelpSummary("The category where Voice Channels will be made.")]
            public ICategoryChannel? VoiceChannelCategory { get; init; }

            [HelpSummary("The category where Voice Chat channels will be made.")]
            public ICategoryChannel? VoiceChatCategory { get; init; }

            [HelpSummary("Purge empty channels after 1 minute automatically.")]
            public bool PurgeEmpty { get; init; } = true;

            [HelpSummary("Show join messages.")] public bool ShowJoinLeave { get; init; } = true;
        }
    }
}