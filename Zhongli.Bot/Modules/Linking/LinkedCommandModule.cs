using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Interactive;
using Zhongli.Services.Linking;
using Zhongli.Services.Utilities;
using static Zhongli.Data.Models.Discord.Message.Linking.UserTargetOptions;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Bot.Modules.Linking;

[Group("command")]
[Alias("custom", "commands")]
[Name("Custom Commands")]
[Summary("Create custom commands")]
public class LinkedCommandModule : InteractiveEntity<LinkedCommand>
{
    private readonly CommandService _commands;
    private readonly LinkedCommandService _linked;
    private readonly ZhongliContext _db;

    public LinkedCommandModule(CommandErrorHandler error, ZhongliContext db,
        CommandService commands, LinkedCommandService linked) : base(error, db)
    {
        _commands = commands;
        _linked   = linked;
        _db       = db;
    }

    [Command("create")]
    [Alias("add", "learn")]
    [Summary("Creates a new custom command.")]
    public async Task LinkAsync(
        [Summary("The name of the custom command.")]
        string name,
        [Remainder] LinkedCommandOptions options)
    {
        var command = new LinkedCommand(name, options);
        await AddCommandAsync(command, options);
    }

    [Command("delete")]
    [Alias("remove", "unlearn")]
    [Summary("Remove a message template.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("view")]
    [Alias("list")]
    [Summary("View message templates.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override bool IsMatch(LinkedCommand entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override EmbedBuilder EntityViewer(LinkedCommand entity)
    {
        var inclusions = entity.Inclusions.Humanize().DefaultIfNullOrWhiteSpace("Everyone");
        var roles = entity.Roles.Humanize().DefaultIfNullOrWhiteSpace("None");
        var description = entity.Description
            .Truncate(EmbedFieldBuilder.MaxFieldValueLength)
            .DefaultIfNullOrWhiteSpace("None");

        var embed = new EmbedBuilder()
            .AddField("Summary", description)
            .AddField("Ephemeral", entity.Ephemeral, true)
            .AddField("Silent", entity.Silent, true)
            .AddField("Cooldown", entity.Cooldown?.Humanize() ?? "None", true)
            .AddField("Allowed", inclusions, true)
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

    protected override async Task RemoveEntityAsync(LinkedCommand entity)
    {
        await _linked.DeleteAsync(entity);
        await _linked.RefreshCommandsAsync(Context.Guild);
    }

    protected override async Task<ICollection<LinkedCommand>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.LinkedCommands;
    }

    private async Task AddCommandAsync(LinkedCommand command, ICriteriaOptions options)
    {
        if (_commands.Search(Context, command.Name).IsSuccess)
            throw new InvalidOperationException("A command with that name already exists.");

        var commands = await GetCollectionAsync();
        var existing = commands.FirstOrDefault(t => t.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) await RemoveEntityAsync(existing);

        command.Inclusions = options.ToCriteria();
        commands.Add(command);

        await _db.SaveChangesAsync();
        await _linked.RefreshCommandsAsync(Context.Guild);

        var embed = EntityViewer(command)
            .WithColor(Color.Green)
            .WithUserAsAuthor(Context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await ReplyAsync(embed: embed.Build());
    }

    [NamedArgumentType]
    public class LinkedCommandOptions : ILinkedCommandOptions
    {
        [HelpSummary("The permissions that the user must have.")]
        public GuildPermission Permission { get; set; } = GuildPermission.None;

        [HelpSummary("The text or category channels this permission will work on.")]
        public IEnumerable<IGuildChannel>? Channels { get; set; }

        [HelpSummary("The users that are allowed to use the command.")]
        public IEnumerable<IGuildUser>? Users { get; set; }

        [HelpSummary("The roles that are allowed to use the command.")]
        public IEnumerable<IRole>? Roles { get; set; }

        [HelpSummary("The scope that the user must have to use this command.")]
        public AuthorizationScope Scope { get; set; }

        [HelpSummary("True to send the message as ephemeral and False to not.")]
        public bool Ephemeral { get; set; }

        [HelpSummary("True to delete the message after the command is executed, False if not.")]
        public bool Silent { get; set; }

        [HelpSummary("The message that will be sent to the user.")]
        public IMessage? Message { get; set; }

        [Description("The description of this custom command that will appear in the help command.")]
        public string? Description { get; set; }

        [Description("The cooldown for this command.")]
        public TimeSpan? Cooldown { get; set; }

        [HelpSummary("The different options that will be used, comma separated.")]
        public UserTargetOptions UserOptions { get; set; }

        [HelpSummary("True to allow mentions and False to not.")]
        public bool AllowMentions { get; set; }

        [HelpSummary("True if you want the message to be live, where it will update its contents continuously.")]
        public bool IsLive { get; set; }

        [HelpSummary("True if you want embed timestamps to use the current time, False if not.")]
        public bool ReplaceTimestamps { get; set; }

        [HelpSummary("The roles that will be added to the user.")]
        public IEnumerable<IRole>? AddRoles { get; set; }

        [HelpSummary("The roles that will be removed from the user.")]
        public IEnumerable<IRole>? RemoveRoles { get; set; }

        [HelpSummary("The roles that will be toggled on the user.")]
        public IEnumerable<IRole>? ToggleRoles { get; set; }
    }
}