using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Logging;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public enum FilterType
{
    Messages,
    Duplicates,
    Attachments,
    Emojis,
    Invites,
    Links,
    Mentions,
    NewLines
}

public class Filtered : ExpirableReprimand
{
    protected Filtered() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public Filtered(ICollection<MessageDeleteLog> messages, TimeSpan? length, ReprimandDetails details)
        : base(length, details)
    {
        Messages = messages;
    }

    public virtual ICollection<MessageDeleteLog> Messages { get; set; } = new List<MessageDeleteLog>();
}