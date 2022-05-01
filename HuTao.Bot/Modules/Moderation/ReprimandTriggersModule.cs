using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
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

    public ReprimandTriggersModule(
        CommandErrorHandler error, HuTaoContext db,
        ModerationService moderation)
        : base(error, db, moderation)
    {
        _error = error;
        _db    = db;
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

    protected override bool IsMatch(ReprimandTrigger entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override EmbedBuilder EntityViewer(ReprimandTrigger trigger) => new EmbedBuilder()
        .WithTitle($"{trigger.Reprimand?.GetTitle()}: {trigger.Id}")
        .AddField("Action", $"{trigger.Reprimand}")
        .AddField("Category", trigger.Category?.Name ?? "None")
        .AddField("Trigger", trigger.GetTriggerDetails())
        .AddField("Active", $"{trigger.IsActive}")
        .AddField("Modified by", trigger.GetModerator());

    protected override async Task RemoveEntityAsync(ReprimandTrigger entity)
    {
        var triggerHasReprimand = _db.Set<Reprimand>()
            .Any(r => r.TriggerId == entity.Id);

        if (triggerHasReprimand)
            entity.IsActive = false;
        else
            _db.Remove(entity);

        await _db.SaveChangesAsync();
    }

    protected override async Task<ICollection<ReprimandTrigger>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules;

        return rules.Triggers.OfType<ReprimandTrigger>().ToArray();
    }

    private async Task TryAddTriggerAsync(
        ReprimandAction action, TriggerSource source, ITrigger? options)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules;
        // if (options?.Category is not null)
        //     options.Category = await _db.Guilds.TrackCategoryAsync(Context.Guild, options.Category);

        var trigger = new ReprimandTrigger(options, source, action);
        var existing = rules.Triggers
            .OfType<ReprimandTrigger>()
            .FirstOrDefault(t => t.IsActive
                && t.Source == source
                && t.Amount == trigger.Amount);

        if (existing is not null) await RemoveEntityAsync(existing);

        rules.Triggers.Add(trigger.WithModerator(Context));
        await _db.SaveChangesAsync();

        var embed = new EmbedBuilder()
            .WithTitle("Trigger added")
            .WithColor(Color.Green)
            .AddField("Action", trigger.Reprimand?.ToString() ?? "None")
            .AddField("Trigger", trigger.GetTriggerDetails())
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

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
}