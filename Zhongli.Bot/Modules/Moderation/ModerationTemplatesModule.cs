using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Moderation.Infractions.Templates;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;
using static Zhongli.Data.Models.Authorization.AuthorizationScope;

namespace Zhongli.Bot.Modules.Moderation;

[Group("template")]
[Alias("templates")]
[Summary(
    "Reprimand templates are a way to quickly reprimand a user using a template. They are executed using the `template` command.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ModerationTemplatesModule : InteractiveEntity<ModerationTemplate>
{
    private readonly ZhongliContext _db;

    public ModerationTemplatesModule(CommandErrorHandler error, ZhongliContext db) : base(error, db) { _db = db; }

    [Command("ban")]
    public async Task BanTemplateAsync(string name, uint deleteDays = 0, TimeSpan? length = null,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new BanTemplate(deleteDays, length, details);

        await AddTemplateAsync(template);
    }

    [Command("kick")]
    public async Task KickTemplateAsync(string name,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new KickTemplate(details);

        await AddTemplateAsync(template);
    }

    [Command("mute")]
    public async Task MuteTemplateAsync(string name, TimeSpan? length = null,
        AuthorizationScope scope = Moderator, [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new MuteTemplate(length, details);

        await AddTemplateAsync(template);
    }

    [Command("note")]
    public async Task NoteTemplateAsync(string name, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new NoteTemplate(details);

        await AddTemplateAsync(template);
    }

    [Command("notice")]
    public async Task NoticeTemplateAsync(string name, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new NoticeTemplate(details);

        await AddTemplateAsync(template);
    }

    [Command("warn")]
    public async Task WarnTemplateAsync(string name, uint amount, AuthorizationScope scope = Moderator,
        [Remainder] string? reason = null)
    {
        var details = new TemplateDetails(name, reason, scope);
        var template = new WarningTemplate(amount, details);

        await AddTemplateAsync(template);
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

    private async Task AddTemplateAsync(ModerationTemplate template)
    {
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