using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Zhongli.Data;
using Zhongli.Data.Config;
using Zhongli.Services.AutoRemoveMessage;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Expirable;
using Zhongli.Services.Image;
using Zhongli.Services.Logging;
using Zhongli.Services.Moderation;
using Zhongli.Services.Quote;
using Zhongli.Services.TimeTracking;

namespace Zhongli.Bot;

public class Bot
{
    private static CancellationTokenSource? _mediatorToken;
    private static readonly TimeSpan ResetTimeout = TimeSpan.FromSeconds(15);
    private CancellationTokenSource _reconnectCts = null!;

    public static async Task Main() => await new Bot().StartAsync();

    private static ServiceProvider ConfigureServices() =>
        new ServiceCollection().AddHttpClient().AddMemoryCache().AddHangfireServer()
            .AddDbContext<ZhongliContext>(ContextOptions)
            .AddMediatR(c => c.Using<ZhongliMediator>().AsTransient(),
                typeof(Bot), typeof(DiscordSocketListener))
            .AddLogging(l => l.AddSerilog())
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize    = ZhongliConfig.Configuration.MessageCacheSize,
                GatewayIntents      = GatewayIntents.All
            }))
            .AddSingleton<CommandService>()
            .AddSingleton<CommandErrorHandler>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<InteractionService>()
            .AddSingleton<InteractionHandlingService>()
            .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(10) })
            .AddSingleton<DiscordSocketListener>()
            .AddScoped<AuthorizationService>()
            .AddScoped<ModerationService>()
            .AddScoped<ModerationLoggingService>()
            .AddScoped<LoggingService>()
            .AddScoped<UserService>()
            .AddScoped<GenshinTimeTrackingService>()
            .AddSingleton<IQuoteService, QuoteService>()
            .AddExpirableServices()
            .AddAutoRemoveMessage()
            .AddCommandHelp()
            .AddImages()
            .BuildServiceProvider();

    private static async Task CheckStateAsync(IDiscordClient client)
    {
        // Client reconnected, no need to reset
        if (client.ConnectionState == ConnectionState.Connected) return;

        Log.Information("Attempting to reset the client");

        var timeout = Task.Delay(ResetTimeout);
        var connect = client.StartAsync();
        var task = await Task.WhenAny(timeout, connect);

        if (task == timeout)
        {
            Log.Fatal("Client reset timed out (task deadlocked?), killing process");
            FailFast();
        }
        else if (connect.IsFaulted)
        {
            Log.Fatal(connect.Exception, "Client reset faulted, killing process");
            FailFast();
        }
        else if (connect.IsCompletedSuccessfully) Log.Information("Client reset successfully!");
    }

    private Task ClientOnConnected()
    {
        Log.Debug("Client reconnected, resetting cancel tokens...");

        _reconnectCts.Cancel();
        _reconnectCts = new CancellationTokenSource();

        Log.Debug("Client reconnected, cancel tokens reset");
        return Task.CompletedTask;
    }

    private Task ClientOnDisconnected(IDiscordClient client)
    {
        // Check the state after <timeout> to see if we reconnected
        Log.Information("Client disconnected, starting timeout task...");
        _ = Task.Delay(ResetTimeout, _reconnectCts.Token).ContinueWith(async _ =>
        {
            Log.Debug("Timeout expired, continuing to check client state...");
            await CheckStateAsync(client);
            Log.Debug("State came back okay");
        });

        return Task.CompletedTask;
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    private static Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error    => LogEventLevel.Error,
            LogSeverity.Warning  => LogEventLevel.Warning,
            LogSeverity.Info     => LogEventLevel.Information,
            LogSeverity.Verbose  => LogEventLevel.Verbose,
            LogSeverity.Debug    => LogEventLevel.Debug,
            _                    => LogEventLevel.Information
        };

        Log.Write(severity, message.Exception, message.Message);

        return Task.CompletedTask;
    }

    private async Task StartAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Hangfire", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

        await using var services = ConfigureServices();

        GlobalConfiguration.Configuration
            .UseActivator(new AspNetCoreJobActivator(
                services.GetRequiredService<IServiceScopeFactory>()))
            .UseSerilogLogProvider()
            .UsePostgreSqlStorage(ZhongliConfig.Configuration.HangfireContext)
            .UseRecommendedSerializerSettings();

        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
        await services.GetRequiredService<InteractionHandlingService>().InitializeAsync();

        var commands = services.GetRequiredService<CommandService>();
        var client = services.GetRequiredService<DiscordSocketClient>();
        var listener = services.GetRequiredService<DiscordSocketListener>();

        _reconnectCts  = new CancellationTokenSource();
        _mediatorToken = new CancellationTokenSource();

        await listener.StartAsync(_mediatorToken.Token);

        client.Disconnected += _ => ClientOnDisconnected(client);
        client.Connected    += ClientOnConnected;

        commands.Log += LogAsync;
        client.Log   += LogAsync;

        await client.LoginAsync(TokenType.Bot, ZhongliConfig.Configuration.Token);
        await client.StartAsync();

        using var server = new BackgroundJobServer();

        await Task.Delay(Timeout.Infinite);
    }

    private static void ContextOptions(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseLazyLoadingProxies()
        .UseNpgsql(ZhongliConfig.Configuration.ZhongliContext);

    private static void FailFast()
        => Environment.Exit(1);
}