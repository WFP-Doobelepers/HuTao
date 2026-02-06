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

[Group("censor-exclusion", "Manage censor exclusions.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveCensorExclusionsModule(HuTaoContext db, IMemoryCache cache)
    : InteractionEntity<Criterion>
{
    [SlashCommand("add-role", "Exclude a role from all censors.")]
    public async Task ExcludeRoleAsync(
        IRole role,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new RoleCriterion(role));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Excluded role {role.Mention} from censors.", ephemeral: ephemeral);
    }

    [SlashCommand("add-user", "Exclude a user from all censors.")]
    public async Task ExcludeUserAsync(
        IUser user,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new UserCriterion(user.Id));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Excluded user {user.Mention} from censors.", ephemeral: ephemeral);
    }

    [SlashCommand("add-channel", "Exclude a channel from all censors.")]
    public async Task ExcludeChannelAsync(
        IGuildChannel channel,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        collection.Add(new ChannelCriterion(channel.Id, channel is ICategoryChannel));
        await db.SaveChangesAsync();
        cache.InvalidateCaches(Context.Guild);
        await FollowupAsync($"Excluded channel {MentionUtils.MentionChannel(channel.Id)} from censors.", ephemeral: ephemeral);
    }

    [SlashCommand("list", "View all censor exclusions.")]
    public async Task ListExclusionsAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a censor exclusion by ID.")]
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
