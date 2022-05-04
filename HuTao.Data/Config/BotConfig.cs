using Discord;

namespace HuTao.Data.Config;

public class BotConfig
{
    public bool AlwaysDownloadUsers { get; set; }

    public GatewayIntents GatewayIntents { get; init; } = GatewayIntents.AllUnprivileged;

    public int MessageCacheSize { get; set; }

    public string HangfireContext { get; init; } = null!;

    public string HuTaoContext { get; init; } = null!;

    public string Prefix { get; init; } = null!;

    public string Token { get; init; } = null!;

    public ulong Guild { get; init; }

    public ulong Liben { get; init; }

    public ulong Owner { get; init; }
}