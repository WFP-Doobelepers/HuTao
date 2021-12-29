using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message;
using Attachment = Zhongli.Data.Models.Discord.Message.Attachment;
using Embed = Zhongli.Data.Models.Discord.Message.Embed;

namespace Zhongli.Data.Models.Logging;

public class MessageLog : ILog, IMessageEntity
{
    protected MessageLog() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public MessageLog(GuildUserEntity user, IUserMessage message)
    {
        LogDate = DateTimeOffset.UtcNow;

        User      = user;
        ChannelId = message.Channel.Id;

        Content     = message.Content;
        Attachments = message.Attachments.Select(a => new Attachment(a)).ToList();
        Embeds      = message.Embeds.Select(e => new Embed(e)).ToList();

        CreatedAt       = message.CreatedAt.ToUniversalTime();
        Timestamp       = message.Timestamp.ToUniversalTime();
        EditedTimestamp = message.EditedTimestamp?.ToUniversalTime();

        MessageId           = message.Id;
        MentionedEveryone   = message.MentionedEveryone;
        MentionedRolesCount = message.MentionedUserIds.Count;
        MentionedUsersCount = message.MentionedUserIds.Count;

        ReferencedMessageId = message.ReferencedMessage?.Id;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="IMessage.MentionedEveryone" />
    public bool MentionedEveryone { get; set; }

    /// <inheritdoc cref="IMessage.CreatedAt" />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc cref="IMessage.Timestamp" />
    public DateTimeOffset Timestamp { get; set; }

    /// <inheritdoc cref="IMessage.EditedTimestamp" />
    public DateTimeOffset? EditedTimestamp { get; set; }

    public virtual GuildEntity Guild { get; set; } = null!;

    public virtual GuildUserEntity User { get; set; } = null!;

    public virtual ICollection<Attachment> Attachments { get; set; } = null!;

    public virtual ICollection<Embed> Embeds { get; set; } = null!;

    public int MentionedRolesCount { get; set; }

    public int MentionedUsersCount { get; set; }

    public virtual MessageLog? UpdatedLog { get; set; }

    /// <inheritdoc cref="IMessage.Content" />
    public string? Content { get; set; }

    public ulong? ReferencedMessageId { get; set; }

    public ulong ChannelId { get; set; }

    public ulong GuildId { get; set; }

    public DateTimeOffset LogDate { get; set; }

    public ulong MessageId { get; set; }

    public ulong UserId { get; set; }
}

public class MessageEntityConfiguration : IEntityTypeConfiguration<MessageLog>
{
    public void Configure(EntityTypeBuilder<MessageLog> builder) => builder.AddUserNavigation(m => m.User);
}