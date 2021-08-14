using System;
using System.Collections.Generic;
using System.Linq;
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
using Zhongli.Services.Core;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules
{
    [Group("role")]
    [Name("Role Management")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public class RoleModule : InteractiveBase
    {
        private readonly TemporaryRoleService _temporary;

        public RoleModule(TemporaryRoleService temporary) { _temporary = temporary; }

        [Command("add")]
        [Summary("Adds specified roles to a user.")]
        public async Task AddRolesAsync(IGuildUser user, params IRole[] roles)
        {
            await user.AddRolesAsync(roles);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
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
                catch (HttpException e)
                {
                    if (e.HttpCode == HttpStatusCode.Forbidden)
                        continue;

                    throw;
                }
            }

            await message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("color")]
        [Summary("Changes specified roles colors.")]
        public async Task ChangeColorsAsync(Color color, params IRole[] roles)
        {
            foreach (var role in roles)
            {
                await role.ModifyAsync(r => r.Color = color);
            }

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("create")]
        [Summary("Creates a role.")]
        public async Task CreateRoleAsync(string name, RoleCreationOptions? options = null)
        {
            var role = await Context.Guild.CreateRoleAsync(name,
                options?.Permissions, options?.Color,
                options?.IsHoisted ?? false,
                options?.IsMentionable ?? false);

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("delete")]
        [Summary("Deletes the specified roles.")]
        public async Task DeleteRolesAsync(params IRole[] roles)
        {
            foreach (var role in roles)
            {
                await role.DeleteAsync();
            }

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("remove")]
        [Summary("Removes specified roles to a user.")]
        public async Task RemoveRolesAsync(IGuildUser user, params IRole[] roles)
        {
            await user.RemoveRolesAsync(roles);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("remove everyone")]
        [Summary("Removes specified roles to everyone.")]
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
                    catch (HttpException e)
                    {
                        if (e.HttpCode == HttpStatusCode.Forbidden)
                            continue;

                        throw;
                    }
                }
            }

            await message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("temporary convert")]
        [Summary("Converts a role into a temporary role.")]
        public async Task TemporaryRoleConvertAsync(IRole role, TimeSpan length)
        {
            await _temporary.CreateTemporaryRoleAsync(role, length);

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
        [Alias("temporary add")]
        [Summary("Creates a temporary role that gets deleted after a specified time.")]
        public async Task TemporaryRoleCreateAsync(string name, TimeSpan length, RoleCreationOptions? options = null)
        {
            var role = await Context.Guild.CreateRoleAsync(name,
                options?.Permissions, options?.Color,
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
            public bool? IsHoisted { get; set; }

            public bool? IsMentionable { get; set; }

            public Color? Color { get; set; }

            public GuildPermissions? Permissions { get; set; }
        }
    }
}