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
public class ModerationCategoryModule : InteractiveEntity<ModerationCategory>
{
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;

    public ModerationCategoryModule(CommandErrorHandler error, HuTaoContext db)
    {
        _error = error;
        _db    = db;
    }

    [Command("add")]
    [Summary("Add a new moderation category.")]
    public async Task AddModerationCategoryAsync(string name, ModerationCategoryOptions? options = null)
    {
        if (name.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyAsync("You cannot add a moderation category with the name `all`.");
            return;
        }

        var category = new ModerationCategory(name, options, (IGuildUser) Context.User);
        var collection = await GetCollectionAsync();

        if (collection.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await ReplyAsync($"A category with the name `{name}` already exists.");
            return;
        }

        collection.Add(category);
        await _db.SaveChangesAsync();
        await ReplyAsync(embed: EntityViewer(category).WithColor(Color.Green).Build());
    }

    [Command("default")]
    [Summary("Sets the default category for reprimands.")]
    public async Task SetDefaultCategoryAsync(
        [Summary("The category to set as the default.")] [CheckCategory(AuthorizationScope.History)]
        ModerationCategory? category = null)
    {
        if (category == ModerationCategory.All)
        {
            await _error.AssociateError(Context, "You cannot set the default category to `All`.");
            return;
        }

        var user = await _db.Users.TrackUserAsync(Context.User, Context.Guild);
        user.DefaultCategory = category == ModerationCategory.None ? null : category;
        await _db.SaveChangesAsync();

        await ReplyAsync($"Default reprimand category set to `{user.DefaultCategory?.Name ?? "None"}`.");
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
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.ModerationCategories;
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