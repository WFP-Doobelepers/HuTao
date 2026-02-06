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
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("variable", "Manage moderation variables.")]
[RequireContext(ContextType.Guild)]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class InteractiveVariablesModule(HuTaoContext db)
    : InteractionEntity<ModerationVariable>
{
    private const uint AccentColor = 0x9B59FF;

    [SlashCommand("add", "Add a new moderation variable.")]
    public async Task AddVariableAsync(
        string name, string value,
        [RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        if (rules.Variables.Any(v => v.Name == name))
        {
            await FollowupAsync($"A variable named `{name}` already exists.", ephemeral: true);
            return;
        }

        var variable = new ModerationVariable(name, value);
        rules.Variables.Add(variable);
        await Db.SaveChangesAsync();

        var embed = EntityViewer(variable).WithColor(Color.Green).Build();
        var container = embed.ToComponentsV2Container(accentColor: AccentColor, maxChars: 3800);
        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary))
            .Build();

        await FollowupAsync(components: components, allowedMentions: AllowedMentions.None, ephemeral: ephemeral);
    }

    [SlashCommand("list", "View all moderation variables.")]
    public async Task ListVariablesAsync([RequireEphemeralScope] bool ephemeral = false)
    {
        await DeferAsync(ephemeral);
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection, ephemeral);
    }

    [SlashCommand("remove", "Remove a moderation variable by ID.")]
    protected override Task RemoveEntityAsync(
        [Autocomplete(typeof(VariableAutocomplete))] string id,
        [RequireEphemeralScope] bool ephemeral = false)
        => base.RemoveEntityAsync(id, ephemeral);

    protected override EmbedBuilder EntityViewer(ModerationVariable entity) => new EmbedBuilder()
        .WithTitle($"{entity.Name}: {entity.Id}")
        .WithDescription(entity.Value);

    protected override string Id(ModerationVariable entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(ModerationVariable entity, bool ephemeral)
    {
        Db.Remove(entity);
        await Db.SaveChangesAsync();
    }

    protected override async Task<ICollection<ModerationVariable>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();
        return rules.Variables;
    }
}
