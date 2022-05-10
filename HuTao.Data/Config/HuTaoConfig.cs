using Microsoft.Extensions.Configuration;

namespace HuTao.Data.Config;

public class HuTaoConfig
{
    private static readonly IConfigurationRoot Secrets
        = new ConfigurationBuilder().AddUserSecrets<HuTaoConfig>().Build();

    public BotConfig Debug { get; init; } = null!;

    public BotConfig Release { get; init; } = null!;

    public static BotConfig Configuration { get; } =
#if DEBUG
        Secrets.GetSection(nameof(Debug)).Get<BotConfig>();
#else
        Secrets.GetSection(nameof(Release)).Get<BotConfig>();
#endif
}