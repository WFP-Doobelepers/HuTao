using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("template")]
[Alias("templates")]
[Summary(
    "Reprimand templates are a way to quickly reprimand a user using a template. They are executed using the `template` command.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ModerationTemplatesModule(HuTaoContext db) : InteractiveEntity<ModerationTemplate>
{
    private const uint AccentColor = 0x9B59FF;

    [Command("ban")]
    public async Task BanTemplateAsync(string name, BanTemplateOptions options)
    {
        var action = new BanAction(options.DeleteDays, options.Length);
        await AddTemplateAsync(name, action, options);
    }

    [Command("kick")]
    public async Task KickTemplateAsync(string name, KickTemplateOptions options)
    {
        var action = new KickAction();
        await AddTemplateAsync(name, action, options);
    }

    [Command("mute")]
    public async Task MuteTemplateAsync(string name, MuteTemplateOptions options)
    {
        var action = new MuteAction(options.Length);
        await AddTemplateAsync(name, action, options);
    }

    [Command("note")]
    public async Task NoteTemplateAsync(string name, NoteTemplateOptions options)
    {
        var action = new NoteAction();
        await AddTemplateAsync(name, action, options);
    }

    [Command("notice")]
    public async Task NoticeTemplateAsync(string name, NoticeTemplateOptions options)
    {
        var action = new NoticeAction();
        await AddTemplateAsync(name, action, options);
    }

    [Command("role")]
    public async Task RoleTemplateAsync(string name, RoleTemplateOptions options)
    {
        if (options is { AddRoles: null, RemoveRoles: null, ToggleRoles: null })
        {
            await ReplyPanelAsync("Moderation Templates", "You must specify at least one role to add, remove, or toggle.");
            return;
        }

        var action = new RoleAction(options.Length, options);
        await AddTemplateAsync(name, action, options);
    }

    [Command("warn")]
    public async Task WarnTemplateAsync(string name, WarnTemplateOptions options)
    {
        var action = new WarningAction(options.Amount);
        await AddTemplateAsync(name, action, options);
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Delete a moderation template by ID.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command]
    [Alias("list", "view")]
    [Summary("View the moderation templates list.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(ModerationTemplate template) => new EmbedBuilder()
        .WithTitle($"{template.Name}: {template.Id}")
        .WithDescription(template.Reason ?? "No reason")
        .AddField("Action", $"{template}".Truncate(EmbedFieldBuilder.MaxFieldValueLength))
        .AddField("Scope", template.Scope.Humanize())
        .AddField("Category", template.Category?.Name ?? "Default");

    protected override string Id(ModerationTemplate entity) => entity.Id.ToString();

    protected override async Task<ICollection<ModerationTemplate>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationTemplates;
    }

    private async Task AddTemplateAsync(string name, ReprimandAction action, ITemplateOptions options)
    {
        var template = new ModerationTemplate(name, action, options);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);

        var existing = guild.ModerationTemplates.FirstOrDefault(t => t.Name == template.Name);
        if (existing is not null) await RemoveEntityAsync(existing);

        guild.ModerationTemplates.Add(template);
        await db.SaveChangesAsync();

        var embed = EntityViewer(template)
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyEmbedWithConfigButtonAsync(embed.Build());
    }

    private async Task ReplyPanelAsync(string title, string body)
    {
        var components = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay($"## {title}\n{body}")
                .WithAccentColor(AccentColor))
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary)
                .WithButton("Open Triggers", "trg:open", ButtonStyle.Secondary))
            .Build();

        await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
    }

    private async Task ReplyEmbedWithConfigButtonAsync(Embed embed)
    {
        var container = embed.ToComponentsV2Container(accentColor: embed.Color?.RawValue ?? AccentColor, maxChars: 3800);

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary)
                .WithButton("Open Triggers", "trg:open", ButtonStyle.Secondary))
            .Build();

        await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
    }

    [NamedArgumentType]
    public class BanTemplateOptions : ITemplateOptions
    {
        public TimeSpan? Length { get; set; }

        public uint DeleteDays { get; set; }

        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Ban;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class NoteTemplateOptions : ITemplateOptions
    {
        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Note;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class NoticeTemplateOptions : ITemplateOptions
    {
        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Warning;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class KickTemplateOptions : ITemplateOptions
    {
        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Kick;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class MuteTemplateOptions : ITemplateOptions
    {
        public TimeSpan? Length { get; set; }

        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Mute;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class WarnTemplateOptions : ITemplateOptions
    {
        public uint Amount { get; set; } = 1;

        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Warning;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }

    [NamedArgumentType]
    public class RoleTemplateOptions : ITemplateOptions, IRoleTemplateOptions
    {
        public TimeSpan? Length { get; set; }

        public IEnumerable<IRole>? AddRoles { get; set; }

        public IEnumerable<IRole>? RemoveRoles { get; set; }

        public IEnumerable<IRole>? ToggleRoles { get; set; }

        public AuthorizationScope Scope { get; set; } = AuthorizationScope.Mute;

        public ModerationCategory? Category { get; set; }

        public string? Reason { get; set; }
    }
}