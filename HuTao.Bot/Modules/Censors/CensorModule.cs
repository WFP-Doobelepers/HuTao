using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Censors;

[Name("Censor")]
[Group("censor")]
[Alias("censors")]
[Summary("Manages censors and what action will be done to the user who triggers them.")]
public class CensorModule : InteractiveTrigger<Censor>
{
    private const string PatternSummary = "The .NET flavor regex pattern to be used.";
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public CensorModule(HuTaoContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    [Command("ban")]
    [Summary("A censor that deletes the message and also bans the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddBanCensorAsync(
        [Summary(PatternSummary)] string pattern,
        [Summary("Amount in days of messages that will be deleted when banned.")]
        uint deleteDays = 0,
        [Summary("The length of the ban. Leave empty for permanent.")]
        TimeSpan? length = null,
        CensorOptions? options = null)
    {
        var trigger = new BanAction(deleteDays, length);
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("add")]
    [Alias("create")]
    [Summary("A censor that deletes the message and does nothing to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddCensorAsync(
        [Summary(PatternSummary)] string pattern,
        CensorOptions? options = null)
    {
        var censor = new Censor(pattern, null, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("kick")]
    [Summary("A censor that deletes the message and also kicks the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddKickCensorAsync(
        [Summary(PatternSummary)] string pattern,
        CensorOptions? options = null)
    {
        var trigger = new KickAction();
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("mute")]
    [Summary("A censor that deletes the message and mutes the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddMuteCensorAsync(
        [Summary(PatternSummary)] string pattern,
        [Summary("The length of the mute. Leave empty for permanent.")]
        TimeSpan? length = null,
        CensorOptions? options = null)
    {
        var trigger = new MuteAction(length);
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("note")]
    [Summary("A censor that deletes the message and does nothing to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddNoteCensorAsync(
        [Summary(PatternSummary)] string pattern,
        CensorOptions? options = null)
    {
        var trigger = new NoteAction();
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("notice")]
    [Summary("A censor that deletes the message and gives a notice.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddNoticeCensorAsync(
        [Summary(PatternSummary)] string pattern,
        CensorOptions? options = null)
    {
        var trigger = new NoticeAction();
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("warning")]
    [Alias("warn")]
    [Summary("A censor that deletes the message and does nothing to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddWarningCensorAsync(
        [Summary(PatternSummary)] string pattern,
        [Summary("The amount of warnings to be given. Defaults to `1`.")]
        uint count = 1,
        CensorOptions? options = null)
    {
        var trigger = new WarningAction(count);
        var censor = new Censor(pattern, trigger, options);

        await AddCensor(censor, options);
        await ReplyCensorAsync(censor);
    }

    [Command("test")]
    [Alias("testword")]
    [Summary("Test whether a word is in the list of censors or not.")]
    [RequireAuthorization(AuthorizationScope.History | AuthorizationScope.Configuration)]
    public async Task TestCensorAsync([Remainder] string word)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        var matches = guild.ModerationRules.Triggers.OfType<Censor>()
            .Where(c => c.Regex().IsMatch(word)).ToList();

        if (matches.Any())
            await PagedViewAsync(matches);
        else
            await ReplyAsync("No matches found.");
    }

    [Command]
    [Alias("list", "view")]
    [Summary("View the censor list.")]
    [RequireAuthorization(AuthorizationScope.History | AuthorizationScope.Configuration)]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(Censor censor) => new EmbedBuilder()
        .WithTitle($"{censor.Reprimand?.GetTitle()} Censor: {censor.Id}")
        .AddField("Pattern", Format.Code(censor.Pattern))
        .AddField("Options", censor.Options.Humanize(), true)
        .AddField("Silent", $"{censor.Silent}", true)
        .AddField("Reprimand", censor.Reprimand?.ToString() ?? "None", true)
        .AddField("Trigger", censor.GetTriggerMode(), true)
        .AddField("Exclusions", censor.Exclusions.Humanize().DefaultIfNullOrEmpty("None"), true)
        .AddField("Active", $"{censor.IsActive}", true)
        .AddField("Modified by", censor.GetModerator(), true);

    protected override string Id(Censor entity) => entity.Id.ToString();

    protected override async Task<ICollection<Censor>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.Triggers.OfType<Censor>().ToList();
    }

    private async Task AddCensor(Censor censor, ICriteriaOptions? options)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

        if (options is not null)
            censor.Exclusions = options.ToCriteria();

        guild.ModerationRules ??= new ModerationRules();
        guild.ModerationRules.Triggers.Add(censor.WithModerator(Context));

        await _db.SaveChangesAsync();
        _cache.InvalidateCaches(Context.Guild);
    }

    private async Task ReplyCensorAsync(Censor censor)
    {
        var embed = EntityViewer(censor);
        await ReplyAsync(embed: embed.Build());
    }

    [NamedArgumentType]
    public class CensorOptions : ICensorOptions, ICriteriaOptions
    {
        [HelpSummary("Silently match and do not delete the message.")]
        public bool Silent { get; set; } = false;

        [HelpSummary("Comma separated regex flags. Defaults to `IgnoreCase`.")]
        public RegexOptions Flags { get; set; } = RegexOptions.IgnoreCase;

        [HelpSummary("The permissions that the user must have.")]
        public GuildPermission Permission { get; set; } = GuildPermission.None;

        [HelpSummary("The text or category channels that will be excluded.")]
        public IEnumerable<IGuildChannel>? Channels { get; set; }

        [HelpSummary("The users that are excluded.")]
        public IEnumerable<IGuildUser>? Users { get; set; }

        [HelpSummary("The roles that are excluded.")]
        public IEnumerable<IRole>? Roles { get; set; }

        [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
        public JudgeType JudgeType { get; set; } = JudgeType.Any;

        [HelpSummary("The name of the category this will be added to.")]
        public ModerationCategory? Category { get; set; }

        [HelpSummary("The behavior in which the reprimand of the censor triggers.")]
        public TriggerMode Mode { get; set; } = TriggerMode.Exact;

        [HelpSummary("The amount of times the censor should be triggered before reprimanding.")]
        public uint Amount { get; set; } = 1;
    }
}