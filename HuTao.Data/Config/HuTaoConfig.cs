using System;
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
        Secrets.GetSection(nameof(Debug)).Get<BotConfig>() ?? throw new InvalidOperationException($"{nameof(Debug)} config is null");
#else
        Secrets.GetSection(nameof(Release)).Get<BotConfig>() ?? throw new InvalidOperationException($"{nameof(Release)} config is null");
#endif
}