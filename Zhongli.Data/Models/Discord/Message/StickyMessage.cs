using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class StickyMessage
{
    public StickyMessage() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public StickyMessage(IMessage message)
    {
        ChannelId = message.Channel.Id;

        Content     = message.Content;
        Attachments = message.Attachments.Select(a => new Attachment(a)).ToList();
        Embeds      = message.Embeds.Select(e => new Embed(e)).ToList();
    }

    public Guid Id { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    /// <inheritdoc cref="IMessage.Attachments" />
    public virtual ICollection<Attachment> Attachments { get; set; }
        = new List<Attachment>();

    /// <inheritdoc cref="IMessage.Embeds" />
    public virtual ICollection<Embed> Embeds { get; set; }
        = new List<Embed>();

    /// <inheritdoc cref="IMessage.Content" />
    public string? Content { get; set; }

    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }
}