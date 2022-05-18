using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Linking;

[Group("button")]
[Name("Button Linking")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class LinkedButtonModule : InteractiveEntity<LinkedButton>
{
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;
    private readonly LinkingService _linking;

    public LinkedButtonModule(CommandErrorHandler error, HuTaoContext db, LinkingService linking)
    {
        _error   = error;
        _linking = linking;
        _db      = db;
    }

    [Command("link")]
    [Summary("Links a message to another message using a button.")]
    public async Task LinkAsync(
        [Summary(
            "The message to link to. " +
            "The message must be sent by the bot in order to be able to edit it. " +
            "If not, a new message will be sent.")]
        IMessage link,
        [Remainder] LinkedMessageOptions options)
    {
        var message = await GetMessageAsync(link, options.Channel);
        var button = await _linking.LinkMessageAsync(message, options);

        if (button is null)
            await _error.AssociateError(Context.Message, "Provide a Message/URL in your button and an Emote/Label.");
        else
            await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Remove a linked button.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("view")]
    [Alias("list")]
    [Summary("View linked buttons.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

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

    protected override Task RemoveEntityAsync(LinkedButton entity) => _linking.DeleteAsync(entity);

    protected override async Task<ICollection<LinkedButton>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.LinkedButtons;
    }

    private async Task<IUserMessage> GetMessageAsync(IMessage message, IMessageChannel? channel)
    {
        if (message is IUserMessage userMessage && message.Author.Id == Context.Client.CurrentUser.Id)
            return userMessage;

        var template = new MessageTemplate(message, null);
        return await template.SendMessageAsync(channel ?? Context.Channel);
    }
}