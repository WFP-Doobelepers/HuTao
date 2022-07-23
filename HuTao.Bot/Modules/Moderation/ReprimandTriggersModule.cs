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
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("trigger")]
[Alias("triggers")]
[Summary("Reprimand triggers.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ReprimandTriggersModule : InteractiveTrigger<ReprimandTrigger>
{
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public ReprimandTriggersModule(CommandErrorHandler error, HuTaoContext db, ModerationService moderation)
    {
        _error      = error;
        _db         = db;
        _moderation = moderation;
    }

    [Command("ban")]
    public async Task BanTriggerAsync(TriggerSource source,
        uint deleteDays = 0, TimeSpan? length = null,
        TriggerOptions? options = null)
    {
        var action = new BanAction(deleteDays, length);
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("kick")]
    public async Task KickTriggerAsync(TriggerSource source, TriggerOptions? options = null)
    {
        var action = new KickAction();
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("mute")]
    public async Task MuteTriggerAsync(TriggerSource source,
        TimeSpan? length = null, TriggerOptions? options = null)
    {
        var action = new MuteAction(length);
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("note")]
    public async Task NoteTriggerAsync(TriggerSource source, TriggerOptions? options = null)
    {
        var action = new NoteAction();
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("notice")]
    public async Task NoticeTriggerAsync(TriggerSource source, TriggerOptions? options = null)
    {
        var action = new NoticeAction();
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("role")]
    public async Task RoleTriggerAsync(TriggerSource source, RoleReprimandOptions options)
    {
        if (options is { AddRoles: null, RemoveRoles: null, ToggleRoles: null })
        {
            await _error.AssociateError(Context, "No roles specified.");
            return;
        }

        var action = new RoleAction(options.Length, options);
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("warn")]
    public async Task WarnTriggerAsync(
        TriggerSource source, uint warnCount = 1,
        TriggerOptions? options = null)
    {
        var action = new WarningAction(warnCount);
        await TryAddTriggerAsync(action, source, options);
    }

    [Command("reprimands")]
    [Alias("history")]
    [Summary("Shows associated reprimands of this trigger.")]
    protected async Task ViewAssociatedReprimandsAsync(string id,
        [Summary("Leave empty to show everything.")] LogReprimandType type = LogReprimandType.All)
    {
        var trigger = await TryFindEntityAsync(id);

        if (trigger is null)
        {
            await _error.AssociateError(Context.Message, EmptyMatchMessage);
            return;
        }

        var reprimands = await _db.Set<Reprimand>().AsAsyncEnumerable()
            .Where(r => r.TriggerId == trigger.Id)
            .ToListAsync();

        await PagedViewAsync(reprimands.OfType(type), r => r.ToEmbedBuilder(true));
    }

    [Command]
    [Alias("list", "view")]
    [Summary("View the reprimand trigger list.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(ReprimandTrigger trigger) => new EmbedBuilder()
        .WithTitle($"{trigger.Reprimand?.GetTitle()}: {trigger.Id}")
        .AddField("Action", $"{trigger.Reprimand}".Truncate(EmbedFieldBuilder.MaxFieldValueLength))
        .AddField("Category", trigger.Category?.Name ?? "None")
        .AddField("Trigger", trigger.GetDetails())
        .AddField("Active", $"{trigger.IsActive}")
        .AddField("Modified by", trigger.GetModerator());

    protected override string Id(ReprimandTrigger entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(ReprimandTrigger entity)
    {
        var triggerHasReprimand = _db.Set<Reprimand>()
            .Any(r => r.TriggerId == entity.Id);

        if (triggerHasReprimand)
            entity.IsActive = false;
        else
            await _moderation.DeleteTriggerAsync(entity, (IGuildUser) Context.User, false);

        await _db.SaveChangesAsync();
    }

    protected override async Task<ICollection<ReprimandTrigger>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        return rules.Triggers.OfType<ReprimandTrigger>().ToList();
    }

    private async Task TryAddTriggerAsync(
        ReprimandAction action, TriggerSource source, ITrigger? options)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        var trigger = new ReprimandTrigger(options, source, action);
        var existing = rules.Triggers
            .OfType<ReprimandTrigger>()
            .Where(t => t.Category?.Id == options?.Category?.Id)
            .FirstOrDefault(t => t.IsActive
                && t.Source == source
                && t.Amount == trigger.Amount);

        if (existing is not null) await RemoveEntityAsync(existing);

        rules.Triggers.Add(trigger.WithModerator(Context));
        await _db.SaveChangesAsync();

        var embed = EntityViewer(trigger).WithColor(Color.Green);
        await ReplyAsync(embed: embed.Build());
    }

    [NamedArgumentType]
    public class TriggerOptions : ITrigger
    {
        [HelpSummary("The name of the category this will be added to.")]
        public ModerationCategory? Category { get; set; }

        [HelpSummary("The behavior in which the reprimand triggers.")]
        public TriggerMode Mode { get; set; } = TriggerMode.Exact;

        [HelpSummary("The amount of times the trigger should be triggered before reprimanding.")]
        public uint Amount { get; set; } = 1;
    }

    [NamedArgumentType]
    public class RoleReprimandOptions : IRoleTemplateOptions, ITrigger
    {
        public TimeSpan? Length { get; set; }

        public IEnumerable<IRole>? AddRoles { get; set; }

        public IEnumerable<IRole>? RemoveRoles { get; set; }

        public IEnumerable<IRole>? ToggleRoles { get; set; }

        public ModerationCategory? Category { get; set; }

        public TriggerMode Mode { get; set; } = TriggerMode.Exact;

        public uint Amount { get; set; } = 1;
    }
}