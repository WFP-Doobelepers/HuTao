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

    public ReprimandTriggersModule(CommandErrorHandler error, HuTaoContext db, ModerationService moderation)
        : base(error, db, moderation)
    {
        _error = error;
        _db    = db;
    }

    [Command("ban")]
    public async Task BanTriggerAsync(uint amount, TriggerSource source,
        uint deleteDays = 0, TimeSpan? length = null,
        TriggerMode mode = TriggerMode.Exact)
    {
        var action = new BanAction(deleteDays, length);
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("kick")]
    public async Task KickTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
    {
        var action = new KickAction();
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("mute")]
    public async Task MuteTriggerAsync(uint amount, TriggerSource source, TimeSpan? length = null,
        TriggerMode mode = TriggerMode.Exact)
    {
        var action = new MuteAction(length);
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("note")]
    public async Task NoteTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
    {
        var action = new NoteAction();
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("notice")]
    public async Task NoticeTriggerAsync(uint amount, TriggerSource source, TriggerMode mode = TriggerMode.Exact)
    {
        var action = new NoticeAction();
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("warn")]
    public async Task WarnTriggerAsync(uint amount, TriggerSource source,
        uint count = 1, TriggerMode mode = TriggerMode.Exact)
    {
        var action = new WarningAction(count);
        await TryAddTriggerAsync(action, amount, source, mode);
    }

    [Command("reprimands")]
    [Alias("history")]
    [Summary("Shows associated reprimands of this trigger.")]
    protected async Task ViewAssociatedReprimandsAsync(string id,
        [Summary("Leave empty to show everything.")]
        LogReprimandType type = LogReprimandType.All)
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

    private async Task TryAddTriggerAsync(ReprimandAction action, uint amount, TriggerSource source,
        TriggerMode mode)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules;

        var options = new TriggerOptions(amount, source, mode);
        var trigger = new ReprimandTrigger(options, options.Source, action);

        var existing = rules.Triggers.OfType<ReprimandTrigger>()
            .Where(t => t.IsActive)
            .FirstOrDefault(t => t.Source == options.Source && t.Amount == trigger.Amount);

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

    private class TriggerOptions : ITrigger
    {
        public TriggerOptions(uint amount, TriggerSource source, TriggerMode mode)
        {
            Mode   = mode;
            Amount = amount;
            Source = source;
        }

        public TriggerSource Source { get; }

        public TriggerMode Mode { get; set; }

        public uint Amount { get; set; }
    }
}