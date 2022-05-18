using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Authorization.AuthorizationScope;

namespace HuTao.Bot.Modules.Moderation;

[Group("template")]
[Alias("templates")]
[Summary(
    "Reprimand templates are a way to quickly reprimand a user using a template. They are executed using the `template` command.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ModerationTemplatesModule : InteractiveEntity<ModerationTemplate>
{
    private readonly HuTaoContext _db;

    public ModerationTemplatesModule(HuTaoContext db) { _db = db; }

    [Command("ban")]
    public async Task BanTemplateAsync(string name, uint deleteDays = 0, TimeSpan? length = null,
        AuthorizationScope scope = Ban, [Remainder] string? reason = null)
    {
        var action = new BanAction(deleteDays, length);
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("kick")]
    public async Task KickTemplateAsync(string name,
        AuthorizationScope scope = Kick, [Remainder] string? reason = null)
    {
        var action = new KickAction();
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("mute")]
    public async Task MuteTemplateAsync(string name, TimeSpan? length = null,
        AuthorizationScope scope = Mute, [Remainder] string? reason = null)
    {
        var action = new MuteAction(length);
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("note")]
    public async Task NoteTemplateAsync(string name, AuthorizationScope scope = Note,
        [Remainder] string? reason = null)
    {
        var action = new NoteAction();
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("notice")]
    public async Task NoticeTemplateAsync(string name, AuthorizationScope scope = Warning,
        [Remainder] string? reason = null)
    {
        var action = new NoticeAction();
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("role")]
    public async Task RoleTemplateAsync(string name, RoleTemplateOptions options)
    {
        if (options is { AddRoles: null, RemoveRoles: null, ToggleRoles: null })
        {
            await ReplyAsync("You must specify at least one role to add, remove, or toggle.");
            return;
        }

        var action = new RoleAction(options.Length, options);
        await AddTemplateAsync(name, action, options.Scope, options.Reason);
    }

    [Command("warn")]
    public async Task WarnTemplateAsync(string name, uint amount, AuthorizationScope scope = Warning,
        [Remainder] string? reason = null)
    {
        var action = new WarningAction(amount);
        await AddTemplateAsync(name, action, scope, reason);
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Delete a moderation template by ID.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command]
    [Alias("list", "view")]
    [Summary("View the reprimand trigger list.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(ModerationTemplate template) => new EmbedBuilder()
        .WithTitle($"{template.Name}: {template.Id}")
        .WithDescription(template.Reason ?? "No reason")
        .AddField("Action", $"{template}".Truncate(EmbedFieldBuilder.MaxFieldValueLength))
        .AddField("Scope", template.Scope.Humanize());

    protected override string Id(ModerationTemplate entity) => entity.Id.ToString();

    protected override async Task<ICollection<ModerationTemplate>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationTemplates;
    }

    private async Task AddTemplateAsync(string name, ReprimandAction action, AuthorizationScope scope, string? reason)
    {
        var template = new ModerationTemplate(name, action, scope, reason);
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

        var existing = guild.ModerationTemplates.FirstOrDefault(t => t.Name == template.Name);
        if (existing is not null) await RemoveEntityAsync(existing);

        guild.ModerationTemplates.Add(template);
        await _db.SaveChangesAsync();

        var embed = EntityViewer(template)
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    [NamedArgumentType]
    public class RoleTemplateOptions : RoleReprimandOptions
    {
        public AuthorizationScope Scope { get; set; } = Mute;

        public string? Reason { get; set; }
    }
}