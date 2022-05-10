using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core.Listeners;
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

    public ModerationTemplatesModule(CommandErrorHandler error, HuTaoContext db) : base(error, db) { _db = db; }

    [Command("ban")]
    public async Task BanTemplateAsync(string name, uint deleteDays = 0, TimeSpan? length = null,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var action = new BanAction(deleteDays, length);
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("kick")]
    public async Task KickTemplateAsync(string name,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var action = new KickAction();
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("mute")]
    public async Task MuteTemplateAsync(string name, TimeSpan? length = null,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var action = new MuteAction(length);
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("note")]
    public async Task NoteTemplateAsync(string name, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var action = new NoteAction();
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("notice")]
    public async Task NoticeTemplateAsync(string name, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var action = new NoticeAction();
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("warn")]
    public async Task WarnTemplateAsync(string name, uint amount, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var action = new WarningAction(amount);
        var details = new TemplateDetails(name, action, reason, scope);

        await AddTemplateAsync(details);
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Delete a moderation template by ID.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command]
    [Alias("list", "view")]
    [Summary("View the reprimand trigger list.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override bool IsMatch(ModerationTemplate entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override EmbedBuilder EntityViewer(ModerationTemplate template) => new EmbedBuilder()
        .WithTitle($"{template.Name}: {template.Id}")
        .WithDescription(template.Reason ?? "No reason")
        .AddField("Action", $"{template}");

    protected override async Task<ICollection<ModerationTemplate>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationTemplates;
    }

    private async Task AddTemplateAsync(TemplateDetails details)
    {
        var template = new ModerationTemplate(details);
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
}