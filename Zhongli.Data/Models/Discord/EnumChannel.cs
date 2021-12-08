using System;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;

namespace Zhongli.Data.Models.Discord;

public abstract class EnumChannel
{
    public Guid Id { get; set; }

    public int IntType { get; set; }

    public ulong ChannelId { get; set; }
}

public class EnumChannel<T> : EnumChannel where T : Enum
{
    protected EnumChannel() { }

    public EnumChannel(T type, IChannel channel)
    {
        Type      = type;
        ChannelId = channel.Id;
    }

    [NotMapped]
    public T Type
    {
        get => (T) (object) IntType;
        set => IntType = (int) (object) value;
    }
}