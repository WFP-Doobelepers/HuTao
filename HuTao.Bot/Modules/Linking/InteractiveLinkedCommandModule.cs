using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;
using static HuTao.Data.Models.Discord.Message.Linking.UserTargetOptions;

namespace HuTao.Bot.Modules.Linking;

[Group("command", "Manage custom commands.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveLinkedCommandModule(HuTaoContext db, LinkedCommandService linked)
    : InteractionEntity<LinkedCommand>
{
    [SlashCommand("list", "View custom commands.")]
    public async Task ListAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("delete", "Remove a custom command by ID.")]
    protected override Task RemoveEntityAsync(string id, [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(LinkedCommand entity)
    {
        var permissions = entity.Authorization.Humanize().DefaultIfNullOrWhiteSpace("Anyone");
        var roles = entity.Roles.Humanize().DefaultIfNullOrWhiteSpace("None");
        var description = entity.Description
            .Truncate(EmbedFieldBuilder.MaxFieldValueLength)
            .DefaultIfNullOrWhiteSpace("None");

        var embed = new EmbedBuilder()
            .AddField("Summary", description)
            .AddField("Ephemeral", entity.Ephemeral, true)
            .AddField("Silent", entity.Silent, true)
            .AddField("Cooldown", entity.Cooldown?.Humanize() ?? "None", true)
            .AddField("Allowed", permissions, true)
            .AddField("Roles", roles, true);

        if (entity.Message is not null)
        {
            embed
                .AddField("Template ID", entity.Message.Id, true)
                .WithTemplateDetails(entity.Message, Context.Guild);
        }

        if (entity.UserOptions is not None)
        {
            embed
                .AddField("DM Users", entity.UserOptions.HasFlag(DmUser), true)
                .AddField("Apply to Self", entity.UserOptions.HasFlag(ApplySelf), true)
                .AddField("Apply to Mentions", entity.UserOptions.HasFlag(ApplyMentions), true);
        }

        return embed.WithTitle($"{entity.Name}: {entity.Id}");
    }

    protected override string Id(LinkedCommand entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(LinkedCommand entity, bool ephemeral)
    {
        await linked.DeleteAsync(entity);
        await linked.RefreshCommandsAsync(Context.Guild);
    }

    protected override async Task<ICollection<LinkedCommand>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.LinkedCommands;
    }
}
