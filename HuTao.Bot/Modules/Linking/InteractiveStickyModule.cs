using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Sticky;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Linking;

[Group("sticky", "Manage sticky messages.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveStickyModule(StickyService sticky) : InteractionEntity<StickyMessage>
{
    [SlashCommand("enable", "Enable a sticky message by ID.")]
    public async Task EnableAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);
        if (entity is null)
        {
            await FollowupAsync("Sticky message not found.", ephemeral: true);
            return;
        }

        await sticky.EnableAsync(entity, (IGuildChannel)Context.Channel);
        await FollowupAsync(
            components: EntityViewer(entity).WithColor(Color.Green).Build().ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    [SlashCommand("disable", "Disable a sticky message by ID.")]
    public async Task DisableAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        var entity = await TryFindEntityAsync(id, collection);
        if (entity is null)
        {
            await FollowupAsync("Sticky message not found.", ephemeral: true);
            return;
        }

        await sticky.DisableAsync(entity);
        await FollowupAsync(
            components: EntityViewer(entity).WithColor(Color.Orange).Build().ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    [SlashCommand("list", "View sticky messages.")]
    public async Task ListAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a sticky message.")]
    protected override Task RemoveEntityAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(StickyMessage entity)
    {
        var template = entity.Template;
        return new EmbedBuilder()
            .AddField("Template ID", template.Id, true)
            .AddField("Channel", $"<#{entity.ChannelId}>", true)
            .WithTemplateDetails(template, Context.Guild)
            .AddField("Active", entity.IsActive, true)
            .AddField("Time Delay", entity.TimeDelay?.Humanize() ?? "None", true)
            .AddField("Count Delay", entity.CountDelay ?? 0, true)
            .WithTitle($"Sticky: {entity.Id}");
    }

    protected override string Id(StickyMessage entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(StickyMessage entity, bool ephemeral)
    {
        await sticky.DeleteAsync(entity);
    }

    protected override Task<ICollection<StickyMessage>> GetCollectionAsync()
        => sticky.GetStickyMessages(Context.Guild);
}
