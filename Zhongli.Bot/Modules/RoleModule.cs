using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Zhongli.Data.Models.Authorization;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Expirable;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules;

[Group("role")]
[Name("Role Management")]
[Summary("Manages roles.")]
[RequireBotPermission(GuildPermission.ManageRoles)]
[RequireUserPermission(GuildPermission.ManageRoles, Group = nameof(RoleModule))]
[RequireAuthorization(AuthorizationScope.Roles, Group = nameof(RoleModule))]
public class RoleModule : ModuleBase<SocketCommandContext>
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
    public Task AddRoleAsync(IGuildUser user, [RequireHierarchy] IRole role, TimeSpan? length = null)
        => length is not null ? AddTemporaryRoleMemberAsync(user, role, length.Value) : AddRolesAsync(user, role);

    [HiddenFromHelp]
    [Command("add")]
    [Summary("Adds specified roles to a user.")]
    public async Task AddRolesAsync(IGuildUser user, [RequireHierarchy] params IRole[] roles)
    {
        await user.AddRolesAsync(roles);

        var embed = new EmbedBuilder()
            .WithDescription(
                $"Added {roles.OrderByDescending(r => r.Position).Humanize(x => x.Mention)} to {user.Mention}.")
            .WithColor(Color.Green);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("add everyone")]
    [Summary("Adds specified roles to everyone.")]
    public async Task AddRolesAsync([RequireHierarchy] params SocketRole[] roles)
    {
        await ReplyAsync("Adding roles, this might take a while...");
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
            .WithDescription(
                $"Added {roles.OrderByDescending(r => r.Position).Humanize(x => x.Mention)} to everyone.")
            .WithColor(Color.Green);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("temporary add")]
    [Alias("tempadd")]
    [Summary("Puts a member into a temporary role.")]
    public async Task AddTemporaryRoleMemberAsync(IGuildUser user, [RequireHierarchy] IRole role, TimeSpan length)
    {
        await _member.AddTemporaryRoleMemberAsync(user, role, length);
        var embed = new EmbedBuilder()
            .WithDescription(new StringBuilder()
                .AppendLine($"Temporarily added {role.Mention} to {user.Mention}.")
                .AppendLine($"Expires {length.ToUniversalTimestamp()}.")
                .ToString())
            .WithColor(Color.Green);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("color")]
    [Summary("Changes specified roles colors.")]
    public async Task ChangeColorsAsync(Color color, [RequireHierarchy] params IRole[] roles)
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
    public async Task DeleteRolesAsync([RequireHierarchy] params IRole[] roles)
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
    public async Task RemoveRolesAsync(IGuildUser user, [RequireHierarchy] params IRole[] roles)
    {
        await user.RemoveRolesAsync(roles);

        var embed = new EmbedBuilder()
            .WithDescription($"Removed {Format.Bold(roles.Humanize())} from {user.Mention}.")
            .WithColor(Color.DarkRed);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("remove everyone")]
    [Summary("Removes specified roles from everyone.")]
    public async Task RemoveRolesAsync([RequireHierarchy] params SocketRole[] roles)
    {
        await ReplyAsync("Removing roles, this might take a while...");
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

    [Command("temporary convert")]
    [Alias("tempconvert")]
    [Summary("Converts a role into a temporary role.")]
    public async Task TemporaryRoleConvertAsync([RequireHierarchy] IRole role, TimeSpan length)
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
    public Task ToggleRoleAsync(IGuildUser user, [RequireHierarchy] IRole role)
        => user.HasRole(role)
            ? RemoveRolesAsync(user, role)
            : AddRolesAsync(user, role);

    [Command("view")]
    [Summary("View the information of specified roles.")]
    public async Task ViewRolesAsync(
        [Summary("Leave empty to show all roles.")]
        params SocketRole[] roles)
    {
        var embeds = roles.Select(ViewRoleInfoAsync);
        await ReplyAsync(embeds: embeds.Select(e => e.Build()).ToArray());
    }

    private EmbedBuilder ViewRoleInfoAsync(SocketRole role)
    {
        var members = role.Members.ToList();
        return new EmbedBuilder()
            .WithGuildAsAuthor(Context.Guild)
            .WithTitle($"{role.Name} ({role.Id})")
            .WithColor(role.Color)
            .AddField("Mention", role.Mention, true)
            .AddField("Members", members.Count, true)
            .AddField("Color", role.Color, true)
            .AddField("Hoisted", role.IsHoisted, true)
            .AddField("Mentionable", role.IsMentionable, true)
            .AddField("Managed", role.IsManaged, true)
            .AddField("Permissions", role.Permissions.ToList().Humanize(p => p.Humanize()).DefaultIfNullOrEmpty("None"))
            .AddItemsIntoFields("Members", role.Members.Take(100), r => r.Mention, " ");
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
}