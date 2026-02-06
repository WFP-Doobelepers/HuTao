using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Data.Models.Discord;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Linking;

[Group("button", "Manage linked buttons.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveLinkedButtonModule(HuTaoContext db, LinkingService linking)
    : InteractionEntity<LinkedButton>
{
    [SlashCommand("list", "View linked buttons.")]
    public async Task ListAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a linked button by ID.")]
    protected override Task RemoveEntityAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(LinkedButton entity)
    {
        var template = entity.Message;
        var button = entity.Button;
        var embed = new EmbedBuilder()
            .AddField("Ephemeral", $"{entity.Ephemeral}", true)
            .AddField("Disabled", button.IsDisabled, true)
            .AddField("Style", button.Style, true)
            .AddField("Emote", button.Emote.DefaultIfNullOrEmpty("None"), true)
            .AddField("Label", button.Label.DefaultIfNullOrEmpty("None"), true)
            .AddField("Url", button.Url.DefaultIfNullOrEmpty("None"), true);

        if (template is not null)
        {
            embed
                .AddField("Template ID", template.Id, true)
                .WithTemplateDetails(template, Context.Guild);
        }

        foreach (var role in entity.Roles.GroupBy(r => r.Behavior))
        {
            embed.AddField($"{role.Key} Roles", role.Humanize(r => r.MentionRole()));
        }

        return embed.WithTitle($"Button: {entity.Id}");
    }

    protected override string Id(LinkedButton entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(LinkedButton entity, bool ephemeral)
    {
        await linking.DeleteAsync(entity);
    }

    protected override async Task<ICollection<LinkedButton>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.LinkedButtons;
    }
}
