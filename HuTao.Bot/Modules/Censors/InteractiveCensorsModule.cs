using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Actions;
using HuTao.Data.Models.Moderation.Infractions.Censors;
using HuTao.Data.Models.Moderation.Infractions.Triggers;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Censors;

[Group("censor", "Manage word censors and their actions.")]
[RequireContext(ContextType.Guild)]
public class InteractiveCensorsModule(HuTaoContext db, IMemoryCache cache)
    : InteractionEntity<Censor>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("add", "Add a censor that deletes the message.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddCensorAsync(
        string pattern,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        bool silent = false,
        TriggerMode mode = TriggerMode.Exact,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!TryValidatePattern(pattern))
        {
            await FollowupAsync("Invalid regex pattern. Please check your syntax.", ephemeral: ephemeral);
            return;
        }
        var options = new SlashCensorOptions { Silent = silent, Mode = mode, Category = category };
        var censor = new Censor(pattern, null, options);
        await AddAndReplyAsync(censor);
    }

    [SlashCommand("warn", "Add a censor that warns the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddWarnCensorAsync(
        string pattern, uint count = 1,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        bool silent = false,
        TriggerMode mode = TriggerMode.Exact,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!TryValidatePattern(pattern))
        {
            await FollowupAsync("Invalid regex pattern. Please check your syntax.", ephemeral: ephemeral);
            return;
        }
        var action = new WarningAction(count);
        var options = new SlashCensorOptions { Silent = silent, Mode = mode, Category = category };
        var censor = new Censor(pattern, action, options);
        await AddAndReplyAsync(censor);
    }

    [SlashCommand("mute", "Add a censor that mutes the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddMuteCensorAsync(
        string pattern, System.TimeSpan? length = null,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        bool silent = false,
        TriggerMode mode = TriggerMode.Exact,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!TryValidatePattern(pattern))
        {
            await FollowupAsync("Invalid regex pattern. Please check your syntax.", ephemeral: ephemeral);
            return;
        }
        var action = new MuteAction(length);
        var options = new SlashCensorOptions { Silent = silent, Mode = mode, Category = category };
        var censor = new Censor(pattern, action, options);
        await AddAndReplyAsync(censor);
    }

    [SlashCommand("ban", "Add a censor that bans the user.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public async Task AddBanCensorAsync(
        string pattern, uint deleteDays = 0, System.TimeSpan? length = null,
        [Autocomplete(typeof(CategoryAutocomplete))]
        ModerationCategory? category = null,
        bool silent = false,
        TriggerMode mode = TriggerMode.Exact,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!TryValidatePattern(pattern))
        {
            await FollowupAsync("Invalid regex pattern. Please check your syntax.", ephemeral: ephemeral);
            return;
        }
        var action = new BanAction(deleteDays, length);
        var options = new SlashCensorOptions { Silent = silent, Mode = mode, Category = category };
        var censor = new Censor(pattern, action, options);
        await AddAndReplyAsync(censor);
    }

    [SlashCommand("test", "Test whether a word matches any censor.")]
    [RequireAuthorization(AuthorizationScope.History | AuthorizationScope.Configuration)]
    public async Task TestCensorAsync(string word, [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        var matches = guild.ModerationRules.Triggers.OfType<Censor>()
            .Where(c => c.Regex().IsMatch(word)).ToList();

        if (matches.Any())
            await PagedViewAsync(matches, ephemeral);
        else
            await FollowupAsync("No matches found.", ephemeral: ephemeral);
    }

    [SlashCommand("list", "View the censor list.")]
    [RequireAuthorization(AuthorizationScope.History | AuthorizationScope.Configuration)]
    public async Task ListAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a censor by ID.")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(CensorAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(Censor censor) => new EmbedBuilder()
        .WithTitle($"{censor.Reprimand?.GetTitle()} Censor: {censor.Id}")
        .AddField("Pattern", Format.Code(censor.Pattern))
        .AddField("Options", censor.Options.Humanize(), true)
        .AddField("Silent", $"{censor.Silent}", true)
        .AddField("Reprimand", censor.Reprimand?.ToString() ?? "None", true)
        .AddField("Active", $"{censor.IsActive}", true)
        .AddField("Modified by", censor.GetModerator(), true);

    protected override string Id(Censor entity) => entity.Id.ToString();

    protected override async Task<ICollection<Censor>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.Triggers.OfType<Censor>().ToList();
    }

    private bool TryValidatePattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private async Task AddAndReplyAsync(Censor censor)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        guild.ModerationRules.Triggers.Add(censor.WithModerator((IGuildUser)Context.User));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        var embed = EntityViewer(censor).WithColor(Color.Green).Build();
        var container = embed.ToComponentsV2Container(accentColor: AccentColor, maxChars: 3800);
        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Triggers", "trg:open", ButtonStyle.Secondary))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None);
    }

    private class SlashCensorOptions : ICensorOptions
    {
        public bool Silent { get; init; }
        public System.Text.RegularExpressions.RegexOptions Flags { get; init; } = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
        public TriggerMode Mode { get; set; } = TriggerMode.Exact;
        public uint Amount { get; set; } = 1;
        public ModerationCategory? Category { get; set; }
    }
}
