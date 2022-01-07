using Microsoft.Extensions.Configuration;

namespace Zhongli.Data.Config;

public class ZhongliConfig
{
    private static readonly IConfigurationRoot Secrets
        = new ConfigurationBuilder().AddUserSecrets<ZhongliConfig>().Build();

    public BotConfig Debug { get; init; } = null!;

    public BotConfig Release { get; init; } = null!;

    public static BotConfig Configuration { get; } =
#if DEBUG
        Secrets.GetSection(nameof(Debug)).Get<BotConfig>();
#else
        Secrets.GetSection(nameof(Release)).Get<BotConfig>();
#endif
}