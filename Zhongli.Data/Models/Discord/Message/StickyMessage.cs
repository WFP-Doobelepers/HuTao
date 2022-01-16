using System;
using System.Diagnostics.CodeAnalysis;
using Discord;
using Zhongli.Data.Models.Discord.Message.Linking;

namespace Zhongli.Data.Models.Discord.Message;

public class StickyMessage
{
    protected StickyMessage() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public StickyMessage(MessageTemplate template, TimeSpan? timeDelay, uint? countDelay, IGuildChannel channel)
    {
        Template   = template;
        TimeDelay  = timeDelay;
        CountDelay = countDelay;
        ChannelId  = channel.Id;
    }

    public Guid Id { get; set; }

    public virtual MessageTemplate Template { get; set; } = null!;

    public TimeSpan? TimeDelay { get; set; }

    public uint? CountDelay { get; set; }

    public ulong ChannelId { get; set; }
}