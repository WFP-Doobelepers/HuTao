using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Bot.Modules.Moderation;

[Group("category")]
public class ModerationCategoryModule(CommandErrorHandler error, HuTaoContext db)
    : InteractiveEntity<ModerationCategory>
{
    private const uint AccentColor = 0x9B59FF;

    [Command("add")]
    [Summary("Add a new moderation category.")]
    public async Task AddModerationCategoryAsync(string name, ModerationCategoryOptions? options = null)
    {
        if (name.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyPanelAsync("Moderation Categories", "You cannot add a moderation category with the name `all`.");
            return;
        }

        var category = new ModerationCategory(name, options, (IGuildUser) Context.User);
        var collection = await GetCollectionAsync();

        if (collection.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await ReplyPanelAsync("Moderation Categories", $"A category with the name `{name}` already exists.");
            return;
        }

        collection.Add(category);
        await db.SaveChangesAsync();
        var embed = EntityViewer(category).WithColor(Color.Green).Build();
        await ReplyEmbedWithConfigButtonAsync(embed);
    }

    [Command("default")]
    [Summary("Sets the default category for reprimands.")]
    public async Task SetDefaultCategoryAsync(
        [Summary("The category to set as the default.")] [CheckCategory(AuthorizationScope.History)]
        ModerationCategory? category = null)
    {
        if (category == ModerationCategory.All)
        {
            await error.AssociateError(Context, "You cannot set the default category to `All`.");
            return;
        }

        var user = await db.Users.TrackUserAsync(Context.User, Context.Guild);
        user.DefaultCategory = category == ModerationCategory.None ? null : category;
        await db.SaveChangesAsync();

        await ReplyPanelAsync(
            "Moderation Categories",
            $"Default reprimand category set to `{user.DefaultCategory?.Name ?? "None"}`.");
    }

    [Command("remove")]
    [Alias("delete", "del")]
    [Summary("Remove a moderation category.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command]
    [Alias("list", "view")]
    [Summary("View the moderation category list.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(ModerationCategory entity) => entity.ToEmbedBuilder();

    protected override string Id(ModerationCategory entity) => entity.Id.ToString();

    protected override async Task<ICollection<ModerationCategory>> GetCollectionAsync()
    {
        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationCategories;
    }

    private async Task ReplyPanelAsync(string title, string body)
    {
        var components = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay($"## {title}\n{body}")
                .WithAccentColor(AccentColor))
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary)
                .WithButton("Open Triggers", "trg:open", ButtonStyle.Secondary))
            .Build();

        await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
    }

    private async Task ReplyEmbedWithConfigButtonAsync(Embed embed)
    {
        var container = embed.ToComponentsV2Container(accentColor: embed.Color?.RawValue ?? AccentColor, maxChars: 3800);

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(new ActionRowBuilder()
                .WithButton("Open Config Panel", "cfg:open", ButtonStyle.Primary)
                .WithButton("Open Triggers", "trg:open", ButtonStyle.Secondary))
            .Build();

        await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
    }

    [NamedArgumentType]
    public class ModerationCategoryOptions : ICriteriaOptions
    {
        [HelpSummary("The permissions that the user must have.")]
        public GuildPermission Permission { get; set; }

        [HelpSummary("The text or category channels this permission will work on.")]
        public IEnumerable<IGuildChannel>? Channels { get; set; }

        [HelpSummary("The users that are allowed to use the command.")]
        public IEnumerable<IGuildUser>? Users { get; set; }

        [HelpSummary("The roles that the user must have.")]
        public IEnumerable<IRole>? Roles { get; set; }

        [HelpSummary("The way how the criteria is judged. Defaults to `Any`.")]
        public JudgeType JudgeType { get; set; } = JudgeType.Any;
    }
}