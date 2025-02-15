using System;
using System.Collections.Concurrent;
using System.Threading;
using Discord;

namespace HuTao.Services.Sticky;

public class StickyMessageDetails
{
    public CancellationTokenSource Token { get; set; } = new();

    public ConcurrentBag<IUserMessage> Messages { get; } = [];

    public DateTimeOffset? LastSent { get; set; }

    public int MessageCount { get; set; }
}