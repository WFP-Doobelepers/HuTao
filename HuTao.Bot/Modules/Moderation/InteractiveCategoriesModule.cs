using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Autocomplete;
using HuTao.Services.Core.Preconditions.Interactions;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("category", "Manage moderation categories.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveCategoriesModule(HuTaoContext db)
    : InteractionEntity<ModerationCategory>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("add", "Add a new moderation category.")]
    public async Task AddCategoryAsync(
        string name,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);

        if (name.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            await FollowupAsync("You cannot add a category named `All`.", ephemeral: true);
            return;
        }

        var collection = await GetCollectionAsync();
        if (collection.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await FollowupAsync($"A category named `{name}` already exists.", ephemeral: true);
            return;
        }

        var category = new ModerationCategory(name, null, (IGuildUser)Context.User);
        collection.Add(category);
        await Db.SaveChangesAsync();

        var embed = EntityViewer(category).WithColor(Color.Green).Build();
        var container = embed.ToComponentsV2Container(accentColor: AccentColor, maxChars: 3800);
        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    [SlashCommand("list", "View all moderation categories.")]
    public async Task ListCategoriesAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a moderation category by ID.")]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(CategoryAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(ModerationCategory entity) => entity.ToEmbedBuilder();

    protected override string Id(ModerationCategory entity) => entity.Id.ToString();

    protected override async Task<ICollection<ModerationCategory>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationCategories;
    }
}
