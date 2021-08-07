using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.TypeReaders;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Group("permissions")]
    [Name("Permissions")]
    public class PermissionsModule : InteractivePromptBase
    {
        private readonly AuthorizationService _auth;
        private readonly CommandErrorHandler _error;
        private readonly ZhongliContext _db;

        public PermissionsModule(AuthorizationService auth, CommandErrorHandler error, ZhongliContext db)
        {
            _auth  = auth;
            _error = error;
            _db    = db;
        }

        [Command("add")]
        [Summary(
            "Add a permission for a specific scope. At least one rule option must be filled. " +
            "Filling multiple options make an Authorization Group. " +
            "An Authorization Group must have all pass before the permission is allowed.")]
        public async Task AddPermissionAsync(AuthorizationScope scope, RuleOptions options)
        {
            var moderator = (IGuildUser) Context.User;
            var rules = new List<Criterion>();

            var channel = options.Channel;

            if (options.User is not null)
                rules.Add(new UserCriterion(options.User.Id));

            if (channel is ITextChannel or ICategoryChannel)
                rules.Add(new ChannelCriterion(channel.Id, channel is ICategoryChannel));

            if (options.Role is not null)
                rules.Add(new RoleCriterion(options.Role.Id));

            if (options.Permission is not null)
                rules.Add(new PermissionCriterion(options.Permission.Value));

            if (rules.Count == 0)
            {
                await _error.AssociateError(Context.Message, "You must provide at least one restricting permission.");
                return;
            }

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.AuthorizationGroups.AddRules(scope, moderator, options.AccessType, rules);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("configure")]
        [Summary("Interactively configure the permissions. This uses a template of having an admin and mod role.")]
        public async Task InteractiveConfigureAsync()
        {
            var fields = new List<EmbedFieldBuilder>();
            foreach (var field in Enum.GetValues<AuthorizationScope>())
            {
                var description = field.GetAttributeOfEnum<DescriptionAttribute>();
                if (!string.IsNullOrWhiteSpace(description?.Description))
                    fields.Add(CreateField(field.ToString(), description.Description));
            }

            static EmbedFieldBuilder CreateField(string name, string value)
                => new EmbedFieldBuilder().WithName(name).WithValue(value);

            var prompts = CreatePromptCollection<ConfigureOptions>()
                .WithPrompt(ConfigureOptions.Admin,
                    "Please enter the role name, ID, or mention the role that will be the admin.")
                .ThatHas(new RoleTypeReader<IRole>())
                .WithPrompt(ConfigureOptions.Moderator,
                    "Please enter the role name, ID, or mention the role that will be the moderator.")
                .ThatHas(new RoleTypeReader<IRole>())
                .WithPrompt(ConfigureOptions.Permissions,
                    "What kind of permissions would you like moderators to have? Separate with spaces.",
                    fields)
                .ThatHas(new EnumFlagsTypeReader<AuthorizationScope>());

            var results = await prompts.GetAnswersAsync();

            var moderator = (IGuildUser) Context.User;
            var user = await _db.Users.TrackUserAsync(moderator);
            var guild = await _auth.AutoConfigureGuild(Context.Guild);

            guild.AuthorizationGroups.AddRules(AuthorizationScope.All, moderator, AccessType.Allow,
                new RoleCriterion(results.Get<IRole>(ConfigureOptions.Admin).Id));

            guild.AuthorizationGroups.AddRules(results.Get<AuthorizationScope>(ConfigureOptions.Permissions),
                moderator, AccessType.Allow,
                new RoleCriterion(results.Get<IRole>(ConfigureOptions.Moderator).Id));

            _db.Update(guild);
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [NamedArgumentType]
        public class RuleOptions
        {
            [HelpSummary("Set 'allow' or 'deny' the matched criteria. Defaults to allow.")]
            public AccessType AccessType { get; set; } = AccessType.Allow;

            [HelpSummary("The permissions that the user must have.")]
            public GuildPermission? Permission { get; set; }

            [HelpSummary("The text or category channel this permission will work on.")]
            public IChannel? Channel { get; set; }

            [HelpSummary("The user(s) that are allowed to use the command.")]
            public IGuildUser? User { get; set; }

            [HelpSummary("The specific role that the user must have.")]
            public IRole? Role { get; set; }
        }

        private enum ConfigureOptions
        {
            Admin,
            Moderator,
            Permissions
        }
    }
}