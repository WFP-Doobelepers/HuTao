using System.Collections.Generic;
using System.Linq;
using HuTao.Data.Config;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HuTao.Tests.Services.Core;

// PRIVACY.md keeps personal information for at most a month, so a config that names no retention keeps 30 days.
public class RetentionConfigTests
{
    private static BotConfig Load(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Release").Get<BotConfig>()!;

    [Fact]
    public void Retention_DefaultsToThirtyDays()
        => Assert.Equal(30, Load(new() { ["Release:Prefix"] = "!" }).Retention.MessageLogDays);

    [Fact]
    public void Retention_ReadsTheConfig()
        => Assert.Equal(90, Load(new() { ["Release:Retention:MessageLogDays"] = "90" }).Retention.MessageLogDays);

    // A retention of zero days would delete each log in the next hourly run, so the bot refuses it at startup.
    [Fact]
    public void RetentionBelowOneDay_IsNamed()
        => Assert.Equal(["Retention:MessageLogDays"],
            Load(new() { ["Release:Retention:MessageLogDays"] = "0" }).Retention.TooShort().ToList());
}
