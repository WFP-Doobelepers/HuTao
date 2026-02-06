using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("templates", "Manage moderation templates.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveTemplatesModule(HuTaoContext db) : InteractionEntity<ModerationTemplate>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("warn", "Create a warning template.")]
    public async Task WarnTemplateAsync(
        string name, string? reason = null, uint amount = 1,
        AuthorizationScope scope = AuthorizationScope.Warning,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var action = new WarningAction(amount);
        await AddTemplateAsync(name, action, reason, scope, category, ephemeral);
    }

    [SlashCommand("mute", "Create a mute template.")]
    public async Task MuteTemplateAsync(
        string name, string? reason = null, TimeSpan? length = null,
        AuthorizationScope scope = AuthorizationScope.Mute,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var action = new MuteAction(length);
        await AddTemplateAsync(name, action, reason, scope, category, ephemeral);
    }

    [SlashCommand("ban", "Create a ban template.")]
    public async Task BanTemplateAsync(
        string name, string? reason = null, uint deleteDays = 0, TimeSpan? length = null,
        AuthorizationScope scope = AuthorizationScope.Ban,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var action = new BanAction(deleteDays, length);
        await AddTemplateAsync(name, action, reason, scope, category, ephemeral);
    }

    [SlashCommand("kick", "Create a kick template.")]
    public async Task KickTemplateAsync(
        string name, string? reason = null,
        AuthorizationScope scope = AuthorizationScope.Kick,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var action = new KickAction();
        await AddTemplateAsync(name, action, reason, scope, category, ephemeral);
    }

    [SlashCommand("note", "Create a note template.")]
    public async Task NoteTemplateAsync(
        string name, string? reason = null,
        AuthorizationScope scope = AuthorizationScope.Note,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var action = new NoteAction();
        await AddTemplateAsync(name, action, reason, scope, category, ephemeral);
    }

    [SlashCommand("list", "View all moderation templates.")]
    public async Task ListTemplatesAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a moderation template by ID.")]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(TemplateAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

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

    private async Task AddTemplateAsync(
        string name, ReprimandAction action, string? reason,
        AuthorizationScope scope, ModerationCategory? category, bool ephemeral)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var existing = guild.ModerationTemplates.FirstOrDefault(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            Db.Remove(existing);
            await Db.SaveChangesAsync();
        }

        var options = new SlashTemplateOptions { Scope = scope, Category = category, Reason = reason };
        var template = new ModerationTemplate(name, action, options);
        guild.ModerationTemplates.Add(template);
        await Db.SaveChangesAsync();

        var embed = EntityViewer(template).WithColor(Color.Green).Build();
        var container = embed.ToComponentsV2Container(accentColor: AccentColor, maxChars: 3800);
        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    private class SlashTemplateOptions : ITemplateOptions
    {
        public AuthorizationScope Scope { get; init; }
        public ModerationCategory? Category { get; init; }
        public string? Reason { get; init; }
    }
}
