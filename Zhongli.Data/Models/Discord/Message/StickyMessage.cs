using System;
using System.Diagnostics.CodeAnalysis;
using Discord;
using Zhongli.Data.Models.Discord.Message.Linking;

namespace Zhongli.Data.Models.Discord.Message;

public interface IStickyMessageOptions
{
    public bool IsActive { get; set; }

    public ITextChannel? Channel { get; set; }

    public TimeSpan? TimeDelay { get; set; }

    public uint? CountDelay { get; set; }
}

public class StickyMessage
{
    protected StickyMessage() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public StickyMessage(MessageTemplate template, IGuildChannel channel, IStickyMessageOptions? options)
    {
        Template  = template;
        ChannelId = channel.Id;

        IsActive   = options?.IsActive ?? true;
        TimeDelay  = options?.TimeDelay;
        CountDelay = options?.CountDelay;
    }

    public Guid Id { get; set; }

    public bool IsActive { get; set; }

    public virtual MessageTemplate Template { get; set; } = null!;

    public TimeSpan? TimeDelay { get; set; }

    public uint? CountDelay { get; set; }

    public ulong ChannelId { get; set; }
}