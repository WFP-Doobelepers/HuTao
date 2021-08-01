using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.VoiceChat;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("configure")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ConfigureModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public ConfigureModule(ZhongliContext db) { _db = db; }

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