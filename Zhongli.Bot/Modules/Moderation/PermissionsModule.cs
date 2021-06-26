using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.Core;
using Zhongli.Services.Interactive;
using Zhongli.Services.Interactive.TypeReaders;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Moderation
{
    [Group("permissions")]
    [Name("Permissions")]
    public class PermissionsModule : InteractivePromptBase
    {
        private readonly AuthorizationService _auth;
        private readonly ZhongliContext _db;

        public PermissionsModule(AuthorizationService auth, ZhongliContext db)
        {
            _auth = auth;
            _db   = db;
        }

        [Command("configure")]
        public async Task InteractiveConfigureAsync()
        {
            var permissionOptionFields = new[]
            {
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.All))
                    .WithValue("All permissions. You probably only want this with admins."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Auto))
                    .WithValue("Configuration of the auto moderation settings."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Moderator))
                    .WithValue("Allows warning, mute, kick, and ban."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Helper))
                    .WithValue("Allows warning and mute."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Warning)).WithValue("Allows warning."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Mute)).WithValue("Allows muting."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Kick)).WithValue("Allows kicking."),
                new EmbedFieldBuilder().WithName(nameof(AuthorizationScope.Ban)).WithValue("Allows banning.")
            };

            var prompts = CreatePromptCollection<ConfigureOptions>()
                .WithPrompt(ConfigureOptions.Admin,
                    "Please enter the role name, ID, or mention the role that will be the admin.")
                .ThatHas(new RoleTypeReader<IRole>())
                .WithPrompt(ConfigureOptions.Moderator,
                    "Please enter the role name, ID, or mention the role that will be the moderator.")
                .ThatHas(new RoleTypeReader<IRole>())
                .WithPrompt(ConfigureOptions.Permissions,
                    "What kind of permissions would you like moderators to have? Separate with spaces.",
                    permissionOptionFields)
                .ThatHas(new EnumFlagsTypeReader<AuthorizationScope>());

            var results = await prompts.GetAnswersAsync();

            var user = await _db.Users.TrackUserAsync((IGuildUser) Context.User);
            var auth = await _auth.AutoConfigureGuild(Context.Guild.Id);

            auth.RoleAuthorizations.Add(new RoleAuthorization
            {
                AddedBy = user,
                Date    = DateTimeOffset.UtcNow,

                RoleId = results.Get<IRole>(ConfigureOptions.Admin).Id,
                Scope  = AuthorizationScope.All
            });

            auth.RoleAuthorizations.Add(new RoleAuthorization
            {
                AddedBy = user,
                Date    = DateTimeOffset.UtcNow,

                RoleId = results.Get<IRole>(ConfigureOptions.Moderator).Id,
                Scope  = results.Get<AuthorizationScope>(ConfigureOptions.Permissions)
            });

            _db.Update(auth);
            await _db.SaveChangesAsync();

            await ReplyAsync($"Finished. {auth.RoleAuthorizations.Count}");
        }

        private enum ConfigureOptions
        {
            Admin,
            Moderator,
            Permissions
        }
    }
}