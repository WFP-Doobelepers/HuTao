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
using HuTao.Services.Core;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Bot.Modules.Censors;

[Group("censor-exclusion", "Manage word filter exemptions.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveCensorExclusionsModule(HuTaoContext db, IMemoryCache cache)
    : InteractionEntity<Criterion>
{
    [SlashCommand("add-role", "Exempt a role from all word filters.")]
    public async Task ExcludeRoleAsync(
        IRole role,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new RoleCriterion(role));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Exempted role {role.Mention} from all word filters.", ephemeral: ephemeral);
    }

    [SlashCommand("add-user", "Exempt a user from all word filters.")]
    public async Task ExcludeUserAsync(
        IUser user,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new UserCriterion(user.Id));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Exempted user {user.Mention} from all word filters.", ephemeral: ephemeral);
    }

    [SlashCommand("add-channel", "Exempt a channel from all word filters.")]
    public async Task ExcludeChannelAsync(
        IGuildChannel channel,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new ChannelCriterion(channel.Id, channel is ICategoryChannel));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Exempted channel {MentionUtils.MentionChannel(channel.Id)} from all word filters.", ephemeral: ephemeral);
    }

    [SlashCommand("list", "View all word filter exemptions.")]
    public async Task ListExclusionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a word filter exemption.")]
    protected override async Task RemoveEntityAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
    {
        await base.RemoveEntityAsync(id, ephemeral);
        cache.InvalidateCaches(Context.Guild);
    }

    protected override EmbedBuilder EntityViewer(Criterion entity) => entity.ToEmbedBuilder();

    protected override string Id(Criterion entity) => entity.Id.ToString();

    protected override async Task<ICollection<Criterion>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.ModerationRules ??= new ModerationRules();
        return guild.ModerationRules.CensorExclusions;
    }
}
