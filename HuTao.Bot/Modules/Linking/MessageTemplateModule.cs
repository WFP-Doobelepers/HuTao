using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Linking;
using Microsoft.EntityFrameworkCore;

namespace HuTao.Bot.Modules.Linking;

[Group("message")]
[Name("Message Template Linking")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class MessageTemplateModule : InteractiveEntity<MessageTemplate>
{
    private readonly CommandErrorHandler _error;
    private readonly HuTaoContext _db;
    private readonly LinkingService _linking;

    public MessageTemplateModule(CommandErrorHandler error, HuTaoContext db, LinkingService linking) : base(error, db)
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

    protected override bool IsMatch(MessageTemplate entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override EmbedBuilder EntityViewer(MessageTemplate entity)
        => new EmbedBuilder().WithTemplateDetails(entity, Context.Guild);

    protected override Task RemoveEntityAsync(MessageTemplate entity) => _linking.DeleteAsync(entity);

    protected override async Task<ICollection<MessageTemplate>> GetCollectionAsync()
    {
        var templates = _db.Set<MessageTemplate>();
        var channels = Context.Guild.Channels.Select(c => c.Id);
        return await templates.Where(t => channels.Contains(t.ChannelId)).ToListAsync();
    }
}