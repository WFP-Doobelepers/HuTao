using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.AutoModeration;

[Group("auto")]
[Name("Auto Moderation")]
[Summary("Manage Auto Moderation rules and what action will be done to the user who triggers them.")]
public class AutoModerationModule : InteractiveTrigger<AutoConfiguration>
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public AutoModerationModule(HuTaoContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    [Command("ban")]
    [Summary("A filter that bans the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddBanSpamAsync(FilterType type, BanConfigurationOptions options)
    {
        var reprimand = new BanAction(options.DeleteDays, options.BanLength);
        var config = GetConfiguration(options, type, reprimand);
        await AddConfigurationAsync(config);
    }

    [Command("kick")]
    [Summary("A filter that kicks the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddKickSpamAsync(FilterType type, AutoConfigurationOptions options)
    {
        var reprimand = new KickAction();
        var config = GetConfiguration(options, type, reprimand);
        await AddConfigurationAsync(config);
    }

    [Command("mute")]
    [Summary("A filter that mutes the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddMuteSpamAsync(FilterType type, MuteConfigurationOptions options)
    {
        var reprimand = new MuteAction(options.MuteLength);
        var config = GetConfiguration(options, type, reprimand);
        await AddConfigurationAsync(config);
    }

    [Command("note")]
    [Summary("A filter that adds a note to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddNoteSpamAsync(FilterType type, AutoConfigurationOptions options)
    {
        var reprimand = new NoteAction();
        var config = GetConfiguration(options, type, reprimand);
        await AddConfigurationAsync(config);
    }

    [Command("notice")]
    [Summary("A filter that adds a notice to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddNoticeSpamAsync(FilterType type, AutoConfigurationOptions options)
    {
        var reprimand = new NoticeAction();
        var config = GetConfiguration(options, type, reprimand);
        await AddConfigurationAsync(config);
    }

    [Command("add")]
    [Alias("create")]
    [Summary("A filter that does not give a reprimand to the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddSpamAsync(FilterType type, AutoConfigurationOptions options)
    {
        var config = GetConfiguration(options, type, null);
        await AddConfigurationAsync(config);
    }

    [Command("warning")]
    [Alias("warn")]
    [Summary("A filter that warns the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddWarningSpamAsync(FilterType type, WarningConfigurationOptions options)
    {
        var reprimand = new WarningAction(options.WarnCount);
        var config = GetConfiguration(options, type, reprimand);

        await AddConfigurationAsync(config);
    }

    [Command]
    [Alias("list", "view")]
    [Summary("Lists all the Auto Moderation rules.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(AutoConfiguration entity)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{entity.GetTitle()} Spam: {entity.Id}")
            .WithDescription(entity.GetDetails())
            .WithColor(entity.Reprimand?.GetColor() ?? Color.Default)
            .AddField("Moderator", entity.GetModerator(), true)
            .AddField("Global", entity.Global, true)
            .AddField("Delete Messages", entity.DeleteMessages, true)
            .AddField("Time Period", entity.Length.Humanize(), true)
            .AddField("Limit", entity.Amount, true)
            .AddField("Minimum Length", entity.MinimumLength, true)
            .AddField("Action", entity.Reprimand?.ToString() ?? "None", true)
            .AddField("Cooldown", entity.Cooldown?.Humanize() ?? "Default", true)
            .AddField("Category", entity.Category?.Name ?? "Default", true)
            .WithTimestamp(entity);

        return entity switch
        {
            DuplicateConfiguration config => embed.WithTitle($"{entity.GetTitle()} {config.Type} Spam: {entity.Id}")
                .AddField("Tolerance", config.Tolerance, true)
                .AddField("Percentage", $"{config.Percentage:P}", true)
                .AddField("Type", config.Type, true),
            MentionConfiguration config => embed
                .AddField("Count Duplicate", config.CountDuplicate, true)
                .AddField("Count Invalid", config.CountInvalid, true)
                .AddField("Count Role Members", config.CountRoleMembers, true),
            NewLineConfiguration config => embed.AddField("Blank Lines Only", config.BlankOnly, true),
            _                           => embed
        };
    }

    protected override string Id(AutoConfiguration entity) => entity.Id.ToString();

    protected override async Task<ICollection<AutoConfiguration>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();

        return guild.ModerationRules.Triggers.OfType<AutoConfiguration>().ToList();
    }

    private static AutoConfiguration GetConfiguration(
        IAutoConfigurationOptions options, FilterType type,
        ReprimandAction? reprimand) => type switch
    {
        FilterType.Messages    => new MessageConfiguration(reprimand, options),
        FilterType.Duplicates  => new DuplicateConfiguration(reprimand, options),
        FilterType.Attachments => new AttachmentConfiguration(reprimand, options),
        FilterType.Emojis      => new EmojiConfiguration(reprimand, options),
        FilterType.Invites     => new InviteConfiguration(reprimand, options),
        FilterType.Links       => new LinkConfiguration(reprimand, options),
        FilterType.Mentions    => new MentionConfiguration(reprimand, options),
        FilterType.NewLines    => new NewLineConfiguration(reprimand, options),
        _                      => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown filter type.")
    };

    private async Task AddConfigurationAsync(AutoConfiguration configuration)
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        configuration.Length = configuration.Length.Clamp(1.Seconds(), 1.Hours());
        configuration.Amount = Math.Clamp(configuration.Amount, 1, 100);

        rules.Triggers.Add(configuration.WithModerator(Context));
        await _db.SaveChangesAsync();
        _cache.InvalidateCaches(Context.Guild);

        await ReplyAsync(embed: EntityViewer(configuration).WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested).Build());
    }
}