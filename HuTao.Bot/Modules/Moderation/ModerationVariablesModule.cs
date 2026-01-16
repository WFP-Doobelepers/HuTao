using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

[Group("variable")]
[Alias("variables", "var")]
[Name("Moderation Variables")]
[Summary("Manage variables for moderation")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class ModerationVariablesModule(HuTaoContext db) : InteractiveEntity<ModerationVariable>
{
    [Command("add")]
    [Summary("Add a new moderation variable.")]
    public async Task AddPermissionAsync(
        [Summary(
            "You can use the variable by doing `$name`, `${name}`, or `${name:arguments}`. " +
            "You can use have names by doing `name|name|name` which uses RegEx." +
            @"You can also force the variable to not be used by prepending `\` at the start like `\$name`.")]
        string name,
        [Summary(
            "The value the variable will be replaced to. You can also use [`${args}`]" +
            "(https://docs.microsoft.com/en-us/dotnet/standard/base-types/substitutions-in-regular-expressions) " +
            "to substitute the user's arguments, for example `[${args}](link)`.")]
        [Remainder]
        string value)
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        var existing = rules.Variables.FirstOrDefault(v => v.Name == name);
        if (existing is not null)
        {
            await ReplyAsync($"A variable with the name `{name}` already exists.");
            return;
        }

        var variable = new ModerationVariable(name, value);
        rules.Variables.Add(variable);

        await db.SaveChangesAsync();
        var embed = EntityViewer(variable)
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
            .Build();

        await ReplyAsync(
            components: embed.ToComponentsV2Message(),
            allowedMentions: AllowedMentions.None);
    }

    [Command]
    [Alias("view", "list")]
    [Summary("View the configured moderation variables.")]
    public async Task ViewPermissionsAsync()
    {
        var collection = await GetCollectionAsync();
        await PagedViewAsync(collection);
    }

    [Command("remove")]
    [Alias("delete", "del")]
    [Summary("Remove a moderation variable.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    protected override EmbedBuilder EntityViewer(ModerationVariable entity) => new EmbedBuilder()
        .WithTitle($"{entity.Name}: {entity.Id}")
        .WithDescription($"{entity.Value}");

    protected override string Id(ModerationVariable entity) => entity.Id.ToString();

    protected override async Task RemoveEntityAsync(ModerationVariable entity)
    {
        db.Remove(entity);
        await db.SaveChangesAsync();
    }

    protected override async Task<ICollection<ModerationVariable>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        var rules = guild.ModerationRules ??= new ModerationRules();

        return rules.Variables;
    }
}