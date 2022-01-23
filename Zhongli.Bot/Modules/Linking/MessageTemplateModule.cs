using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Interactive;
using Zhongli.Services.Linking;

namespace Zhongli.Bot.Modules.Linking;

[Group("message")]
[Name("Message Template Linking")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class MessageTemplateModule : InteractiveEntity<MessageTemplate>
{
    private readonly CommandErrorHandler _error;
    private readonly LinkingService _linking;
    private readonly ZhongliContext _db;

    public MessageTemplateModule(CommandErrorHandler error, ZhongliContext db, LinkingService linking) : base(error, db)
    {
        _error   = error;
        _linking = linking;
        _db      = db;
    }

    [Command("link")]
    [Summary("Links a message to another message template using a button.")]
    public async Task LinkAsync(
        [Summary("The message template to link to.")]
        string id,
        [Remainder] LinkedMessageOptions options)
    {
        var template = await TryFindEntityAsync(id, await GetCollectionAsync());
        if (template is null) return;

        var button = await _linking.LinkTemplateAsync(Context, template, options);

        if (button is null)
            await _error.AssociateError(Context.Message, "Provide a Message/URL in your button and an Emote/Label.");
        else
            await Context.Message.AddReactionAsync(new Emoji("✅"));
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Remove a message template.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("view")]
    [Alias("list")]
    [Summary("View message templates.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override (string Title, StringBuilder Value) EntityViewer(MessageTemplate entity)
        => (entity.Id.ToString(), entity.GetTemplateDetails(Context.Guild));

    protected override bool IsMatch(MessageTemplate entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override Task RemoveEntityAsync(MessageTemplate entity) => _linking.DeleteAsync(entity);

    protected override async Task<ICollection<MessageTemplate>> GetCollectionAsync()
    {
        var templates = _db.Set<MessageTemplate>();
        var channels = Context.Guild.Channels.Select(c => c.Id);
        return await templates.Where(t => channels.Contains(t.ChannelId)).ToListAsync();
    }
}