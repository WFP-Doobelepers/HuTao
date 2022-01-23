using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Interactive;
using Zhongli.Services.Linking;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Linking;

[Group("button")]
[Name("Button Linking")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class LinkedButtonModule : InteractiveEntity<LinkedButton>
{
    private readonly CommandErrorHandler _error;
    private readonly LinkingService _linking;
    private readonly ZhongliContext _db;

    public LinkedButtonModule(CommandErrorHandler error, ZhongliContext db, LinkingService linking) : base(error, db)
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
        var message = await GetMessageAsync(link);
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

    protected override (string Title, StringBuilder Value) EntityViewer(LinkedButton entity)
        => (entity.Id.ToString(), GetLinkedButtonDetails(entity));

    protected override bool IsMatch(LinkedButton entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override Task RemoveEntityAsync(LinkedButton entity) => _linking.DeleteAsync(entity);

    protected override async Task<ICollection<LinkedButton>> GetCollectionAsync()
    {
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        return guild.LinkedButtons;
    }

    private StringBuilder GetLinkedButtonDetails(LinkedButton entity)
    {
        var template = entity.Message;
        var builder = new StringBuilder()
            .AppendLine($"▌Ephemeral: {entity.Ephemeral}");

        if (template is not null)
            builder.Append(template.GetTemplateDetails(Context.Guild));

        var roles = entity.Roles.GroupBy(r => r.Behavior);
        foreach (var role in roles)
        {
            builder.AppendLine($"▌{role.Key}: {role.Humanize(r => r.MentionRole())}");
        }

        return builder;
    }

    private async Task<IUserMessage> GetMessageAsync(IMessage message)
    {
        if (message is IUserMessage userMessage && message.Author.Id == Context.Client.CurrentUser.Id)
            return userMessage;

        var template = new MessageTemplate(message, null);
        return await template.SendMessageAsync(Context.Channel);
    }
}