using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Data.Models.Moderation.Auto.Exclusions;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.AutoModeration;

[Group("auto-exclusion", "Manage auto-moderation exemptions.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveModerationExclusionsModule(HuTaoContext db, IMemoryCache cache)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add-role", "Exempt a role from auto-moderation.")]
    public async Task ExcludeRoleAsync(
        IRole role,
        [Summary(description: "Optional auto config to apply to.")] [Autocomplete(typeof(AutoConfigAutocomplete))]
        string? configuration = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await FindConfigAsync(configuration);
        var criterion = new RoleCriterion(role);
        var exclusion = new CriterionExclusion(criterion, config);
        await AddExclusionAsync(exclusion);
        await FollowupAsync($"Exempted role {role.Mention} from auto-moderation.", ephemeral: ephemeral);
    }

    [SlashCommand("add-user", "Exempt a user from auto-moderation.")]
    public async Task ExcludeUserAsync(
        IUser user,
        [Summary(description: "Optional auto config to apply to.")] [Autocomplete(typeof(AutoConfigAutocomplete))]
        string? configuration = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await FindConfigAsync(configuration);
        var criterion = new UserCriterion(user.Id);
        var exclusion = new CriterionExclusion(criterion, config);
        await AddExclusionAsync(exclusion);
        await FollowupAsync($"Exempted user {user.Mention} from auto-moderation.", ephemeral: ephemeral);
    }

    [SlashCommand("add-channel", "Exempt a channel from auto-moderation.")]
    public async Task ExcludeChannelAsync(
        IGuildChannel channel,
        [Summary(description: "Optional auto config to apply to.")] [Autocomplete(typeof(AutoConfigAutocomplete))]
        string? configuration = null,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var config = await FindConfigAsync(configuration);
        var criterion = new ChannelCriterion(channel.Id, channel is ICategoryChannel);
        var exclusion = new CriterionExclusion(criterion, config);
        await AddExclusionAsync(exclusion);
        await FollowupAsync($"Exempted channel {MentionUtils.MentionChannel(channel.Id)} from auto-moderation.", ephemeral: ephemeral);
    }

    [SlashCommand("list", "View all auto-moderation exemptions.")]
    public async Task ListExclusionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var exclusions = await GetCollectionAsync();

        if (exclusions.Count == 0)
        {
            await FollowupAsync("No auto-moderation exemptions configured.", ephemeral: true);
            return;
        }

        var embeds = exclusions
            .Take(10)
            .Select(e => new EmbedBuilder()
                .WithTitle($"Exemption: {e.Id}")
                .WithDescription(e switch
                {
                    CriterionExclusion ce => $"Criterion: {ce.Criterion}",
                    EmojiExclusion ee     => $"Emoji: {ee.Emoji}",
                    LinkExclusion le      => $"Link: {le.Link.Uri}",
                    _                     => e.GetType().Name
                })
                .AddField("Configuration", e.Configuration?.Id.ToString() ?? "Global")
                .Build())
            .ToArray();

        await FollowupAsync(embeds: embeds, ephemeral: ephemeral);
    }

    [SlashCommand("remove", "Remove an auto-moderation exemption.")]
    public async Task RemoveExclusionAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        if (!Guid.TryParse(id, out var guid))
        {
            await FollowupAsync("That ID doesn't look right. Try copying it from the list.", ephemeral: true);
            return;
        }

        var exclusions = await GetCollectionAsync();
        var entity = exclusions.FirstOrDefault(e => e.Id == guid);
        if (entity is null)
        {
            await FollowupAsync("Exemption not found. It may have already been removed.", ephemeral: true);
            return;
        }

        exclusions.Remove(entity);
        db.Remove(entity);
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);

        await FollowupAsync($"Exemption `{id}` removed.", ephemeral: ephemeral);
    }

    private async Task AddExclusionAsync(ModerationExclusion exclusion)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        guild.ModerationRules.Exclusions.Add(exclusion);
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
    }

    private async Task<AutoConfiguration?> FindConfigAsync(string? configId)
    {
        if (string.IsNullOrEmpty(configId)) return null;
        if (!Guid.TryParse(configId, out var id)) return null;

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.Triggers.OfType<AutoConfiguration>()
            .FirstOrDefault(c => c.Id == id);
    }

    private async Task<ICollection<ModerationExclusion>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.Exclusions;
    }
}
