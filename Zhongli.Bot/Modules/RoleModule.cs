using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Expirable;
using Zhongli.Services.Utilities;
using Zhongli.Services.Core.TypeReaders;
using static Zhongli.Bot.Modules.RoleModule.RoleFilters;

namespace Zhongli.Bot.Modules
{
    [Group("role")]
    [Name("Role Management")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public class RoleModule : InteractiveBase
    {
        private readonly TemporaryRoleMemberService _member;
        private readonly TemporaryRoleService _role;

        public RoleModule(TemporaryRoleMemberService member, TemporaryRoleService role)
        {
            _member = member;
            _role   = role;
        }

        [Command("add")]
        [Summary("Adds specified role to a user.")]
        public Task AddRoleAsync(IGuildUser user, IRole role, TimeSpan? length = null)
            => length is not null ? AddTemporaryRoleMemberAsync(user, role, length.Value) : AddRolesAsync(user, role);

        [HiddenFromHelp]
        [Command("add")]
        [Summary("Adds specified roles to a user.")]
        public async Task AddRolesAsync(IGuildUser user, params IRole[] roles)
        {
            await user.AddRolesAsync(roles);

            var embed = new EmbedBuilder()
                .WithDescription($"Added {Format.Bold(roles.Humanize())} to {user.Mention}.")
                .WithColor(Color.Green);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("add everyone")]
        [Summary("Adds specified roles to everyone.")]
        public async Task AddRolesAsync(params SocketRole[] roles)
        {
            var message = await ReplyAsync("Adding roles, this might take a while...");

            if (!Context.Guild.HasAllMembers)
                await Context.Guild.DownloadUsersAsync();

            foreach (var user in Context.Guild.Users)
            {
                try
                {
                    await user.AddRolesAsync(roles);
                }
                catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
                {
                    // Ignored
                }
            }

            var embed = new EmbedBuilder()
                .WithDescription($"Added {Format.Bold(roles.Humanize())} to everyone.")
                .WithColor(Color.Green);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("add filtered")]
        [Summary("Add specified roles to filtered users")]
        public async Task AddRolesAsync(IReadOnlyCollection<IRole> roles, RoleFilters options)
        {
            var message = await ReplyAsync("Adding roles, this might take a while...");
            var users = Context.Guild.Users;
            var fails = 0;

            var rules = options.GetRules();
            var result = options.FilterMode switch
            {
                FilterType.All => users.Where(u => rules.All(rule => rule(u))),
                FilterType.Any or _ => users.Where(u => rules.Any(rule => rule(u)))
            };

            if (options.Invert ?? false)
                result = users.Except(result);

            var filtered = result.ToList();

            foreach (var user in filtered)
            {
                try
                {
                    await user.AddRolesAsync(roles);
                }
                catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
                {
                    fails += 1;
                }
            }

            var embed = new EmbedBuilder()
                .WithDescription($"Added {Format.Bold(roles.Humanize())} to {filtered.Count() - fails} user(s). ({fails} failed)")
                .WithColor(Color.Green);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("temporary add")]
        [Alias("tempadd")]
        [Summary("Puts a member into a temporary role.")]
        public async Task AddTemporaryRoleMemberAsync(IGuildUser user, IRole role, TimeSpan length)
        {
            await _member.AddTemporaryRoleMemberAsync(user, role, length);
            await ReplyAsync(
                $"Added {Format.Bold(role.Name)} to {Format.Bold(user.GetFullUsername())} that ends {length.ToUniversalTimestamp()}.");
        }

        [Command("color")]
        [Summary("Changes specified roles colors.")]
        public async Task ChangeColorsAsync(Color color, params IRole[] roles)
        {
            foreach (var role in roles)
            {
                await role.ModifyAsync(r => r.Color = color);
            }

            var embed = new EmbedBuilder()
                .WithDescription("Color changed successfully")
                .WithColor(color);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("create")]
        [Summary("Creates a role.")]
        public async Task CreateRoleAsync(string name, RoleCreationOptions? options = null)
        {
            var guildPermissions = options?.Permissions?.ToGuildPermissions();
            var role = await Context.Guild.CreateRoleAsync(name,
                guildPermissions, options?.Color,
                options?.IsHoisted ?? false,
                options?.IsMentionable ?? false);

            var permissions = role.Permissions.ToList().Select(p => p.Humanize());
            var embed = new EmbedBuilder()
                .WithColor(role.Color)
                .WithDescription($"Created the following role: {Format.Bold(role.Name)} with the provided options.")
                .AddField("Hoisted", role.IsHoisted, true)
                .AddField("Mentionable", role.IsMentionable, true)
                .AddField("Color", role.Color, true)
                .AddItemsIntoFields("Permissions", permissions, ", ");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("delete")]
        [Summary("Deletes the specified roles.")]
        public async Task DeleteRolesAsync(params IRole[] roles)
        {
            foreach (var role in roles)
            {
                await role.DeleteAsync();
            }

            var embed = new EmbedBuilder()
                .WithDescription($"Deleted the following role(s): {Format.Bold(roles.Humanize())} from the guild.")
                .WithColor(Color.DarkRed);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("remove")]
        [Summary("Removes specified roles to a user.")]
        public async Task RemoveRolesAsync(IGuildUser user, params IRole[] roles)
        {
            await user.RemoveRolesAsync(roles);

            var embed = new EmbedBuilder()
                .WithDescription($"Removed {Format.Bold(roles.Humanize())} from {user.Mention}.")
                .WithColor(Color.DarkRed);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("remove everyone")]
        [Summary("Removes specified roles from everyone.")]
        public async Task RemoveRolesAsync(params SocketRole[] roles)
        {
            var message = await ReplyAsync("Removing roles, this might take a while...");
            foreach (var role in roles)
            {
                foreach (var member in role.Members)
                {
                    try
                    {
                        await member.RemoveRoleAsync(role);
                    }
                    catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
                    {
                        // Ignored
                    }
                }
            }

            var embed = new EmbedBuilder()
                .WithDescription($"Removed {Format.Bold(roles.Humanize())} from everyone.")
                .WithColor(Color.DarkRed);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("remove filtered")]
        [Summary("Removes specified roles to filtered users")]
        public async Task RemoveRolesAsync(IReadOnlyCollection<IRole> roles, RoleFilters options)
        {
            var message = await ReplyAsync("Removing roles, this might take a while...");
            var users = Context.Guild.Users;
            var fails = 0;

            var rules = options.GetRules();
            var result = options.FilterMode switch
            {
                FilterType.All => users.Where(u => rules.All(rule => rule(u))),
                FilterType.Any or _ => users.Where(u => rules.Any(rule => rule(u)))
            };

            if (options.Invert ?? false)
                result = users.Except(result);

            var filtered = result.ToList();

            foreach (var role in roles)
            {
                foreach (var user in filtered)
                {
                    try
                    {
                        await user.RemoveRoleAsync(role);
                    }
                    catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
                    {
                        fails += 1;
                    }
                }
            }

            var embed = new EmbedBuilder()
                .WithDescription($"Removed {Format.Bold(roles.Humanize())} from {filtered.Count() - fails} user(s). ({fails} failed)")
                .WithColor(Color.DarkRed);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("temporary convert")]
        [Alias("tempconvert")]
        [Summary("Converts a role into a temporary role.")]
        public async Task TemporaryRoleConvertAsync(IRole role, TimeSpan length)
        {
            await _role.CreateTemporaryRoleAsync(role, length);

            var embed = new EmbedBuilder()
                .WithTitle("Temporary Role")
                .WithDescription($"Created a temporary role that will expire {length.ToDiscordTimestamp()}")
                .AddField("Role", role.Mention, true)
                .AddField("Mentionable", role.IsMentionable, true)
                .AddField("Hoisted", role.IsHoisted, true)
                .WithCurrentTimestamp()
                .WithUserAsAuthor(Context.Message.Author, AuthorOptions.Requested | AuthorOptions.UseFooter);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("temporary create")]
        [Alias("tempcreate")]
        [Summary("Creates a temporary role that gets deleted after a specified time.")]
        public async Task TemporaryRoleCreateAsync(string name, TimeSpan length, RoleCreationOptions? options = null)
        {
            var permissions = options?.Permissions?.ToGuildPermissions();
            var role = await Context.Guild.CreateRoleAsync(name,
                permissions, options?.Color,
                options?.IsHoisted ?? false,
                options?.IsMentionable ?? false);

            await TemporaryRoleConvertAsync(role, length);
        }

        [Command]
        [Summary("Adds or removes the specified roles to a user.")]
        public Task ToggleRoleAsync(IGuildUser user, IRole role)
            => user.HasRole(role)
                ? RemoveRolesAsync(user, role)
                : AddRolesAsync(user, role);

        [Command("view")]
        [Summary("View the information of specified roles.")]
        public async Task ViewRolesAsync(
            [Summary("Leave empty to show all roles.")]
            params SocketRole[] roles)
        {
            switch (roles.Length)
            {
                case 0:
                    await ViewRolesInfoAsync(Context.Guild.Roles);
                    break;
                case 1:
                    await ViewRoleInfoAsync(roles[0]);
                    break;
                default:
                    await ViewRolesInfoAsync(roles);
                    break;
            }
        }

        private static EmbedFieldBuilder CreateRoleEmbedField(SocketRole role)
        {
            var content = new StringBuilder()
                .AppendLine($"▌Mention: {role.Mention}")
                .AppendLine($"▌Members: {role.Members.Count()}")
                .AppendLine($"▌Color: {role.Color}")
                .AppendLine($"▌Hoisted: {role.IsHoisted}")
                .AppendLine($"▌Mentionable: {role.IsMentionable}")
                .AppendLine($"▌Permissions: {role.Permissions.ToList().Humanize(p => p.Humanize())}");

            return new EmbedFieldBuilder()
                .WithName($"{role.Name} ({role.Id})")
                .WithValue(content.ToString());
        }

        private async Task ViewRoleInfoAsync(SocketRole role)
        {
            var members = role.Members.ToList();

            var embed = new EmbedBuilder()
                .WithGuildAsAuthor(Context.Guild)
                .WithTitle($"{role.Name} ({role.Id})")
                .AddField("Mention", role.Mention, true)
                .AddField("Members", members.Count, true)
                .AddField("Color", role.Color, true)
                .AddField("Hoisted", role.IsHoisted, true)
                .AddField("Mentionable", role.IsMentionable, true)
                .AddField("Managed", role.IsManaged, true)
                .AddField("Permissions", role.Permissions.ToList().Humanize(p => p.Humanize()))
                .AddItemsIntoFields("Members", members, r => r.Mention, " ");

            await ReplyAsync(embed: embed.Build());
        }

        private async Task ViewRolesInfoAsync(IEnumerable<SocketRole> roles)
        {
            var fields = roles.Select(CreateRoleEmbedField);

            var message = new PaginatedMessage
            {
                Pages  = fields,
                Author = new EmbedAuthorBuilder().WithGuildAsAuthor(Context.Guild),
                Options = new PaginatedAppearanceOptions
                {
                    DisplayInformationIcon = false
                }
            };

            await PagedReplyAsync(message);
        }

        [NamedArgumentType]
        public class RoleCreationOptions
        {
            [HelpSummary("Hoist the role in the member list")]
            public bool? IsHoisted { get; set; }

            [HelpSummary("Make the role mentionable")]
            public bool? IsMentionable { get; set; }

            [HelpSummary("Choose the color of the role")]
            public Color? Color { get; set; }

            [HelpSummary("List of permissions")] public IEnumerable<GuildPermission>? Permissions { get; set; }
        }

        [NamedArgumentType]
        public class RoleFilters
        {
            public enum FilterType
            {
                [HelpSummary("Match any of the rules.")]
                Any,

                [HelpSummary("Match all of the rules.")]
                All
            }
 
            [HelpSummary("Invert the results.")] public bool? Invert { get; set; }

            [HelpSummary("Include users with bots.")]
            public bool? IsBot { get; set; }

            [HelpSummary("Include users who have a nickname.")]
            public bool? IsNicknameSet { get; set; }

            [HelpSummary("Include users who boost the server.")]
            public bool? IsBooster { get; set; }

            [HelpSummary("Defaults to 'Any'.")] public FilterType FilterMode { get; set; }

            [HelpSummary("Include users that contain these roles.")]
            public IEnumerable<IRole>? Roles { get; set; }

            [HelpSummary("Include users that contain these permissions.")]
            public IEnumerable<GuildPermission>? Permissions { get; set; }

            [HelpSummary("Include users whose username contains this string.")]
            public string? UserContains { get; set; }

            [HelpSummary("Include users whose username ends with this string.")]
            public string? UserEndsWith { get; set; }

            [HelpSummary("Include users whose username starts with this string.")]
            public string? UserStartsWith { get; set; }

            [HelpSummary("Include users whose username matches this regex pattern. Ignores case.")]
            public string? UserRegexPattern { get; set; }

            [HelpSummary("Include users whose nickname contains this string.")]
            public string? NickContains { get; set; }

            [HelpSummary("Include users whose nickname ends with this string.")]
            public string? NickEndsWith { get; set; }

            [HelpSummary("Include users whose nickname starts with this string.")]
            public string? NickStartsWith { get; set; }

            [HelpSummary("Include users whose nickname matches this regex pattern. Ignores case.")]
            public string? NickRegexPattern { get; set; }

            [HelpSummary("Include users whose nickname contains this string, and if they don't have one, their username.")]
            public string? NameContains { get; set; }

            [HelpSummary("Include users whose nickname ends with this string, and if they don't have one, their username.")]
            public string? NameEndsWith { get; set; }

            [HelpSummary("Include users whose nickname starts with this string, and if they don't have one, their username.")]
            public string? NameStartsWith { get; set; }

            [HelpSummary("Include users whose nickname matches this regex pattern, and if they don't have one, their username. Ignores case.")]
            public string? NameRegexPattern { get; set; }

            [HelpSummary("Include users who joined the server after the specified datetime.")]
            public string? JoinedAfter { get; set; }

            [HelpSummary("Include users who joined the server before the specified datetime.")]
            public string? JoinedBefore { get; set; }

            [HelpSummary("Include users who joined the server at the specified datetime.")]
            public string? JoinedAt { get; set; }

            [HelpSummary("Include users who joined the server at or after the specified datetime.")]
            public string? JoinedAtAfter { get; set; }

            [HelpSummary("Include users who joined the server at or before the specified datetime.")]
            public string? JoinedAtBefore { get; set; }

            [HelpSummary("Include users whose Discord account was created after the specified datetime.")]
            public string? CreatedAfter { get; set; }

            [HelpSummary("Include users whose Discord account was created before the specified datetime.")]
            public string? CreatedBefore { get; set; }

            [HelpSummary("Include users whose Discord account was created at the specified datetime.")]
            public string? CreatedAt { get; set; }

            [HelpSummary("Include users whose Discord account was created at or after the specified datetime.")]
            public string? CreatedAtAfter { get; set; }

            [HelpSummary("Include users whose Discord account was created at or before the specified datetime.")]
            public string? CreatedAtBefore { get; set; }

            [HelpSummary("Include users who boosted the server after the specified datetime.")]
            public string? BoosterAfter { get; set; }

            [HelpSummary("Include users who boosted the server before the specified datetime.")]
            public string? BoosterBefore { get; set; }

            [HelpSummary("Include users who boosted the server at the specified datetime.")]
            public string? BoosterAt { get; set; }

            [HelpSummary("Include users who boosted the server at or after the specified datetime.")]
            public string? BoosterAtAfter { get; set; }

            [HelpSummary("Include users who boosted the server at or before the specified datetime.")]
            public string? BoosterAtBefore { get; set; }

            public IEnumerable<Func<IGuildUser, bool>> GetRules()
            {
                if (IsBot is not null)
                {
                    yield return u =>
                    {
                        var isBot = u.IsBot || u.IsWebhook;
                        return IsBot.Value == isBot;
                    };
                }

                if (IsNicknameSet is not null)
                {
                    yield return u =>
                    {
                        var isNickSet = u.Nickname is not null;
                        return IsNicknameSet.Value == isNickSet;
                    };
                }

                if (IsBooster is not null)
                {
                    yield return u =>
                    {
                        var isBooster = u.PremiumSince is not null;
                        return IsBooster.Value == isBooster;
                    };
                }

                if (Roles is not null)
                {
                    yield return u =>
                        Roles.Select(r => r.Id).Intersect(u.RoleIds).Any();
                }

                if (Permissions is not null)
                {
                    yield return u =>
                        Permissions.Select(p => p).Intersect(u.GuildPermissions.ToList()).Any();
                }

                if (NameContains is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= u.Username;
                        return user_nick.Contains(NameContains);
                    };

                if (NameEndsWith is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= u.Username;
                        return user_nick.EndsWith(NameEndsWith);
                    };

                if (NameStartsWith is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= u.Username;
                        return user_nick.StartsWith(NameStartsWith);
                    };

                if (NameRegexPattern is not null)
                {
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= u.Username;
                        return Regex.IsMatch(user_nick, NameRegexPattern,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    };
                }

                if (UserContains is not null)
                    yield return u => u.Username.Contains(UserContains);

                if (UserEndsWith is not null)
                    yield return u => u.Username.EndsWith(UserEndsWith);

                if (UserStartsWith is not null)
                    yield return u => u.Username.StartsWith(UserStartsWith);

                if (UserRegexPattern is not null)
                {
                    yield return u => Regex.IsMatch(u.Username, UserRegexPattern,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                }

                if (NickContains is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= "";
                        return user_nick.Contains(NickContains);
                    };

                if (NickEndsWith is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= "";
                        return user_nick.EndsWith(NickEndsWith);
                    };

                if (NickStartsWith is not null)
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= "";
                        return user_nick.StartsWith(NickStartsWith);
                    };

                if (NickRegexPattern is not null)
                {
                    yield return u =>
                    {
                        var user_nick = u.Nickname;
                        user_nick ??= "";
                        return Regex.IsMatch(user_nick, NickRegexPattern,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    };
                }

                if (JoinedAfter is not null)
                    yield return u => u.JoinedAt > DateTimeOffset.Parse(JoinedAfter);

                if (JoinedBefore is not null)
                    yield return u => u.JoinedAt < DateTimeOffset.Parse(JoinedBefore);

                if (JoinedAt is not null)
                    yield return u => u.JoinedAt == DateTimeOffset.Parse(JoinedAt);

                if (JoinedAtAfter is not null)
                    yield return u => u.JoinedAt >= DateTimeOffset.Parse(JoinedAtAfter);

                if (JoinedAtBefore is not null)
                    yield return u => u.JoinedAt <= DateTimeOffset.Parse(JoinedAtBefore);

                if (CreatedAfter is not null)
                    yield return u => u.CreatedAt > DateTimeOffset.Parse(CreatedAfter);

                if (CreatedBefore is not null)
                    yield return u => u.CreatedAt < DateTimeOffset.Parse(CreatedBefore);

                if (CreatedAt is not null)
                    yield return u => u.CreatedAt == DateTimeOffset.Parse(CreatedAt);

                if (CreatedAtAfter is not null)
                    yield return u => u.CreatedAt >= DateTimeOffset.Parse(CreatedAtAfter);

                if (CreatedAtBefore is not null)
                    yield return u => u.CreatedAt <= DateTimeOffset.Parse(CreatedAtBefore);

                if (BoosterAfter is not null)
                    yield return u => u.PremiumSince > DateTimeOffset.Parse(BoosterAfter);

                if (BoosterBefore is not null)
                    yield return u => u.PremiumSince < DateTimeOffset.Parse(BoosterBefore);

                if (BoosterAt is not null)
                    yield return u => u.PremiumSince == DateTimeOffset.Parse(BoosterAt);

                if (BoosterAtAfter is not null)
                    yield return u => u.PremiumSince >= DateTimeOffset.Parse(BoosterAtAfter);

                if (BoosterAtBefore is not null)
                    yield return u => u.PremiumSince <= DateTimeOffset.Parse(BoosterAtBefore);
            }
        }
    }
}