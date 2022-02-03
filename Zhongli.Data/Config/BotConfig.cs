﻿namespace Zhongli.Data.Config;

public class BotConfig
{
    public bool AlwaysDownloadUsers { get; set; }

    public int MessageCacheSize { get; set; }

    public string HangfireContext { get; init; } = null!;

    public string Prefix { get; init; } = null!;

    public string Token { get; init; } = null!;

    public string ZhongliContext { get; init; } = null!;

    public ulong Guild { get; init; }

    public ulong Owner { get; init; }
}