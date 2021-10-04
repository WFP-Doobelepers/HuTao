using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Core.TypeReaders;
using Zhongli.Services.Interactive;
using Zhongli.Services.Interactive.Functions;
using Zhongli.Services.Moderation;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Moderation
{
    [Group("permissions")]
    [Name("Permissions")]
    [Alias("perms", "perm")]
    [Summary("Manages guild permissions.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class PermissionsModule : InteractiveEntity<AuthorizationGroup>
    {
        private readonly AuthorizationService _auth;
        private readonly CommandErrorHandler _error;
        private readonly ZhongliContext _db;

        public PermissionsModule(AuthorizationService auth, CommandErrorHandler error, ZhongliContext db) : base(error,
            db)
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
            var rules = options.ToCriteria();
            if (rules.Count == 0)
            {
                await _error.AssociateError(Context.Message, "You must provide at least one restricting permission.");
                return;
            }

            var moderator = (IGuildUser) Context.User;
            var group = new AuthorizationGroup(scope, options.AccessType, rules).WithModerator(moderator);

            await AddEntityAsync(group);
        }

        [Command("configure")]
        [Alias("config")]
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
            _ = await _db.Users.TrackUserAsync(moderator);
            var guild = await _auth.AutoConfigureGuild(Context.Guild);

            guild.AuthorizationGroups.AddRules(AuthorizationScope.All, moderator, AccessType.Allow,
                new RoleCriterion(results.Get<IRole>(ConfigureOptions.Admin)));

            guild.AuthorizationGroups.AddRules(results.Get<AuthorizationScope>(ConfigureOptions.Permissions),
                moderator, AccessType.Allow,
                new RoleCriterion(results.Get<IRole>(ConfigureOptions.Moderator)));

            _db.Update(guild);
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command]
        [Alias("view", "list")]
        [Summary("View the configured authorization groups.")]
        public async Task ViewPermissionsAsync()
        {
            var collection = await GetCollectionAsync();
            await PagedViewAsync(collection);
        }

        [Command("remove")]
        [Alias("delete", "del")]
        [Summary("Remove an authorization group.")]
        protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

        protected override (string Title, StringBuilder Value) EntityViewer(AuthorizationGroup entity)
        {
            var details = GetAuthorizationGroupDetails(entity);
            return (entity.Id.ToString(), details);
        }

        protected override bool IsMatch(AuthorizationGroup entity, string id)
            => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

        protected override async Task RemoveEntityAsync(AuthorizationGroup censor)
        {
            _db.RemoveRange(censor.Collection);
            _db.Remove(censor.Action);
            _db.Remove(censor);

            await _db.SaveChangesAsync();
        }

        protected override async Task<ICollection<AuthorizationGroup>> GetCollectionAsync()
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            return guild.AuthorizationGroups;
        }

        private static StringBuilder GetAuthorizationGroupDetails(AuthorizationGroup group)
        {
            var sb = new StringBuilder()
                .AppendLine($"▌Scope: {Format.Bold(group.Scope.Humanize())}")
                .AppendLine($"▌Type: {Format.Bold(group.Access.Humanize())}")
                .AppendLine($"▌Moderator: {group.GetModerator()}")
                .AppendLine($"▌Date: {group.GetDate()}");

            foreach (var rules in group.Collection.ToLookup(g => g.GetCriterionType()))
            {
                string GetGroupingName() => rules.Key.Name
                    .Replace(nameof(Criterion), string.Empty)
                    .Pluralize().Humanize(LetterCasing.Title);

                sb.AppendLine($"▌▌{GetGroupingName()}: {rules.Humanize()}");
            }

            return sb;
        }

        private async Task<AuthorizationGroup?> TryFindAuthorizationGroup(string id,
            CancellationToken cancellationToken = default)
        {
            if (id.Length < 2)
                return null;

            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild, cancellationToken);
            var group = await guild.AuthorizationGroups.ToAsyncEnumerable()
                .Where(r => r.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase))
                .ToListAsync(cancellationToken);

            if (group.Count <= 1)
                return group.Count == 1 ? group.First() : null;

            var lines = group.Select(r
                => new StringBuilder()
                    .AppendLine($"{r.Id}")
                    .Append(GetAuthorizationGroupDetails(r))
                    .ToString());

            var embed = new EmbedBuilder()
                .WithTitle("Multiple authorization groups found. Reply with the number of the group that you want.")
                .AddItemsIntoFields("Reprimands", lines);

            await ReplyAsync(embed: embed.Build());

            var containsCriterion = new FuncCriterion(m =>
                int.TryParse(m.Content, out var selection)
                && selection < group.Count && selection > -1);

            var selected = await NextMessageAsync(containsCriterion, token: cancellationToken);
            return selected is null ? null : group.ElementAtOrDefault(int.Parse(selected.Content));
        }

        [NamedArgumentType]
        public class RuleOptions : ICriteriaOptions
        {
            [HelpSummary("Set 'allow' or 'deny' the matched criteria. Defaults to allow.")]
            public AccessType AccessType { get; set; } = AccessType.Allow;

            [HelpSummary("The permissions that the user must have.")]
            public GuildPermission Permission { get; set; }

            [HelpSummary("The text or category channels this permission will work on.")]
            public IEnumerable<IGuildChannel>? Channels { get; set; }

            [HelpSummary("The users that are allowed to use the command.")]
            public IEnumerable<IGuildUser>? Users { get; set; }

            [HelpSummary("The roles that the user must have.")]
            public IEnumerable<IRole>? Roles { get; set; }
        }

        private enum ConfigureOptions
        {
            Admin,
            Moderator,
            Permissions
        }
    }
}
