using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using Zhongli.Data.Models.Discord.Message.Components;
using Embed = Zhongli.Data.Models.Discord.Message.Embeds.Embed;

namespace Zhongli.Data.Models.Discord.Message;

public class MessageTemplate
{
    protected MessageTemplate() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public MessageTemplate(IMessage message, bool allowMentions, bool replaceTimeStamps)
    {
        AllowMentions     = allowMentions;
        ReplaceTimestamps = replaceTimeStamps;

        Content     = message.Content;
        Attachments = message.Attachments.Select(a => new Attachment(a)).ToList();
        Embeds      = message.Embeds.Select(e => new Embed(e)).ToList();
    }

    public Guid Id { get; set; }

    public bool AllowMentions { get; set; }

    public bool ReplaceTimestamps { get; set; }

    /// <inheritdoc cref="IMessage.Components" />
    public virtual ICollection<ActionRow> Components { get; set; }
        = new List<ActionRow>();

    /// <inheritdoc cref="IMessage.Attachments" />
    public virtual ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();

    /// <inheritdoc cref="IMessage.Embeds" />
    public virtual ICollection<Embed> Embeds { get; set; }
        = new List<Embed>();

    /// <inheritdoc cref="IMessage.Content" />
    public string? Content { get; set; }
}