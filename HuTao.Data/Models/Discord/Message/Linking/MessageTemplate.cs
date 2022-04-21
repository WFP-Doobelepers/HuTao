using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using HuTao.Data.Models.Discord.Message.Components;
using Embed = HuTao.Data.Models.Discord.Message.Embeds.Embed;

namespace HuTao.Data.Models.Discord.Message.Linking;

public class MessageTemplate
{
    protected MessageTemplate() { }

    public MessageTemplate(IMessage message, IMessageTemplateOptions? options)
    {
        ChannelId         = message.Channel.Id;
        MessageId         = message.Id;
        AllowMentions     = options?.AllowMentions ?? false;
        IsLive            = options?.IsLive ?? false;
        ReplaceTimestamps = options?.ReplaceTimestamps ?? false;
        SuppressEmbeds    = options?.SuppressEmbeds ?? false;

        UpdateTemplate(message);
    }

    public Guid Id { get; set; }

    public bool AllowMentions { get; set; }

    public bool IsLive { get; set; }

    public bool ReplaceTimestamps { get; set; }

    public bool SuppressEmbeds { get; set; }

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

    /// <inheritdoc cref="IMessage.Channel" />
    public ulong ChannelId { get; set; }

    /// <inheritdoc cref="IMessage.Id" />
    public ulong MessageId { get; set; }

    public void UpdateTemplate(IMessage message)
    {
        Content     = message.Content;
        Attachments = message.Attachments.Select(a => new Attachment(a)).ToList();
        Embeds      = message.Embeds.Select(e => new Embed(e)).ToList();
    }
}