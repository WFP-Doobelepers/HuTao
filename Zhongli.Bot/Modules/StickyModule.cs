using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Preconditions.Commands;
using Zhongli.Services.Interactive;
using Zhongli.Services.Sticky;

namespace Zhongli.Bot.Modules;

[Name("Sticky Messages")]
[Group("sticky")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class StickyModule : InteractiveEntity<StickyMessage>
{
    private readonly StickyService _sticky;

    public StickyModule(CommandErrorHandler error, ZhongliContext db, StickyService sticky) : base(error, db)
    {
        _sticky = sticky;
    }

    [Command("add")]
    [RequireContext(ContextType.Guild)]
    public async Task AddStickyMessage(
        [Summary("The message that the sticky message will have. This will copy the embed structure as is.")]
        IMessage message,
        [Summary("The various options your sticky message will have.")]
        StickyMessageOptions? options = null)
    {
        var channel = options?.Channel ?? (ITextChannel) Context.Channel;
        var template = new MessageTemplate(message, options?.AllowMentions ?? false, options?.ResetTimestamps ?? false);
        var sticky = new StickyMessage(template, options?.TimeDelay, options?.CountDelay, channel);

        await _sticky.AddStickyMessage(sticky, Context.Guild);
        await _sticky.SendStickyMessage(sticky, channel);
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Remove a sticky message.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("view")]
    [Alias("list")]
    [Summary("View sticky messages.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override (string Title, StringBuilder Value) EntityViewer(StickyMessage entity)
        => (entity.Id.ToString(), GetStickyMessageDetails(entity));

    protected override bool IsMatch(StickyMessage entity, string id)
        => entity.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase);

    protected override Task<ICollection<StickyMessage>> GetCollectionAsync()
        => _sticky.GetStickyMessages(Context.Guild);

    private static StringBuilder GetStickyMessageDetails(StickyMessage entity)
    {
        var template = entity.Template;
        var builder = new StringBuilder()
            .AppendLine($"▌Channel: <#{entity.ChannelId}>")
            .AppendLine($"▌Content: {template.Content}")
            .AppendLine($"▌Embeds: {template.Embeds.Count}");

        var embed = template.Embeds.FirstOrDefault();
        if (embed is not null)
        {
            builder
                .AppendLine($"▌▌Title: {embed.Title}")
                .AppendLine($"▌▌Description: {embed.Description}");
        }

        return builder;
    }

    [NamedArgumentType]
    public class StickyMessageOptions
    {
        [HelpSummary("True to allow mentions and False to not.")]
        public bool AllowMentions { get; set; }

        [HelpSummary("True if you want embed timestamps to use the current time, false if not.")]
        public bool ResetTimestamps { get; set; }

        [HelpSummary("Optionally the text channel that the sticky message will be sent to.")]
        public ITextChannel? Channel { get; set; }

        [HelpSummary("The time delay to wait before sending another sticky.")]
        public TimeSpan? TimeDelay { get; set; }

        [HelpSummary("The message count delay to wait before sending another sticky.")]
        public uint? CountDelay { get; set; }
    }
}