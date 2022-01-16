using System;
using System.Collections.Concurrent;
using System.Threading;
using Discord;

namespace Zhongli.Services.Sticky;

public class StickyMessageDetails
{
    public CancellationTokenSource Token { get; set; } = new();

    public ConcurrentQueue<IUserMessage> Messages { get; } = new();

    public DateTimeOffset? LastSent { get; set; }

    public IMessage? LiveMessage { get; set; }

    public int MessageCount { get; set; }
}