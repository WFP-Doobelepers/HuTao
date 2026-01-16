using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.PostgreSql;
using HuTao.Data;
using HuTao.Data.Config;
using HuTao.Services.AutoRemoveMessage;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Evaluation;
using HuTao.Services.Expirable;
using HuTao.Services.Image;
using HuTao.Services.Linking;
using HuTao.Services.Logging;
using HuTao.Services.Moderation;
using HuTao.Services.Quote;
using HuTao.Services.Sticky;
using HuTao.Services.TimeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace HuTao.Bot;

public class Bot
{
    private static CancellationTokenSource? _mediatorToken;
    private static readonly TimeSpan ResetTimeout = TimeSpan.FromSeconds(15);
    private CancellationTokenSource _reconnectCts = null!;

    public static async Task Main()
    {
        Log.Logger = CreateLogger();
        RegisterExceptionLogging();

        try
        {
            Log.Information("Starting HuTao bot");
            await new Bot().StartAsync();
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
        {
            Log.Fatal(ex, "HuTao bot terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static ServiceProvider ConfigureServices() =>
        new ServiceCollection()
            .AddLogging(l => l.AddSerilog())
            .AddHttpClient().AddMemoryCache().AddHangfireServer()
            .AddDbContext<HuTaoContext>(ContextOptions)
            .AddMediatR(c =>
            {
                c.MediatorImplementationType = typeof(HuTaoMediator);
                c.RegisterServicesFromAssemblies(typeof(HuTaoMediator).Assembly, typeof(Bot).Assembly);
            })
            .AddSingleton(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = HuTaoConfig.Configuration.AlwaysDownloadUsers,
                MessageCacheSize    = HuTaoConfig.Configuration.MessageCacheSize,
                GatewayIntents      = HuTaoConfig.Configuration.GatewayIntents
            })
            .AddSingleton(new InteractionServiceConfig
            {
                AutoServiceScopes = false,
                UseCompiledLambda = true
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<DiscordRestClient>(x => x.GetRequiredService<DiscordSocketClient>().Rest)
            .AddSingleton<CommandService>()
            .AddSingleton<CommandErrorHandler>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<InteractionService>()
            .AddSingleton<InteractionHandlingService>()
            .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(10) })
            .AddSingleton<DiscordSocketListener>()
            .AddScoped<AuthorizationService>()
            .AddScoped<EvaluationService>()
            .AddScoped<ModerationService>()
            .AddScoped<ModerationLoggingService>()
            .AddScoped<LoggingService>()
            .AddScoped<UserService>()
            .AddScoped<StickyService>()
            .AddScoped<LinkingService>()
            .AddScoped<LinkedCommandService>()
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
        if (client.ConnectionState is ConnectionState.Connected or ConnectionState.Connecting)
        {
            Log.Information("Client is already connected or connecting, skipping reset.");
            return;
        }

        Log.Information("Attempting to reset the client (state: {ConnectionState})", client.ConnectionState);

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
            Log.Fatal(connect.Exception as Exception ?? new InvalidOperationException(),
                "Client reset faulted, killing process");
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

        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }

    private async Task StartAsync()
    {
        await using var services = ConfigureServices();

        GlobalConfiguration.Configuration
            .UseActivator(new AspNetCoreJobActivator(
                services.GetRequiredService<IServiceScopeFactory>()))
            .UseSerilogLogProvider()
            .UsePostgreSqlStorage(x => x.UseNpgsqlConnection(HuTaoConfig.Configuration.HangfireContext))
            .UseRecommendedSerializerSettings();

        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
        await services.GetRequiredService<InteractionHandlingService>().InitializeAsync();

        var commands = services.GetRequiredService<CommandService>();
        var interaction = services.GetRequiredService<InteractionService>();
        var client = services.GetRequiredService<DiscordSocketClient>();
        var listener = services.GetRequiredService<DiscordSocketListener>();

        _reconnectCts  = new CancellationTokenSource();
        _mediatorToken = new CancellationTokenSource();

        await listener.StartAsync(_mediatorToken.Token);

        client.Disconnected += _ => ClientOnDisconnected(client);
        client.Connected    += ClientOnConnected;

        client.Log      += LogAsync;
        commands.Log    += LogAsync;
        interaction.Log += LogAsync;

        await client.LoginAsync(TokenType.Bot, HuTaoConfig.Configuration.Token);
        await client.StartAsync();

        using var server = new BackgroundJobServer();

        await Task.Delay(Timeout.Infinite);
    }

    private static void ContextOptions(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseLazyLoadingProxies()
        .UseNpgsql(HuTaoConfig.Configuration.HuTaoContext);

    private static void FailFast()
    {
        try
        {
            Log.CloseAndFlush();
        }
        finally
        {
            Environment.Exit(1);
        }
    }

    private static ILogger CreateLogger()
    {
        var environment = GetEnvironmentName();
        var minimumLevel = GetMinimumLogLevel();
        var logDirectory = GetLogDirectory();

        Directory.CreateDirectory(logDirectory);

        var fileFormatter = new RenderedCompactJsonFormatter();
        var baseFile = Path.Combine(logDirectory, "hutao-bot-.ndjson");
        var warnFile = Path.Combine(logDirectory, "hutao-bot-warn-.ndjson");

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "HuTao")
            .Enrich.WithProperty("Service", "Bot")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Async(a => a.File(
                fileFormatter,
                baseFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 100_000_000,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)))
            .WriteTo.Async(a => a.File(
                fileFormatter,
                warnFile,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100_000_000,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)));

        var lokiUrl = Environment.GetEnvironmentVariable("HUTAO_LOKI_URL");
        if (!string.IsNullOrWhiteSpace(lokiUrl))
        {
            config.WriteTo.GrafanaLoki(
                lokiUrl,
                labels: new[]
                {
                    new LokiLabel { Key = "app", Value = "hutao" },
                    new LokiLabel { Key = "service", Value = "bot" },
                    new LokiLabel { Key = "environment", Value = environment }
                },
                propertiesAsLabels: new[] { "SourceContext" });
        }

        var logger = config.CreateLogger();
        logger.Information("Logging initialized (Environment={Environment}, Level={Level}, Directory={Directory})",
            environment, minimumLevel, logDirectory);

        return logger;
    }

    private static LogEventLevel GetMinimumLogLevel()
    {
        var configured = Environment.GetEnvironmentVariable("HUTAO_LOG_LEVEL");
        if (Enum.TryParse(configured, ignoreCase: true, out LogEventLevel level))
            return level;

#if DEBUG
        return LogEventLevel.Debug;
#else
        return LogEventLevel.Information;
#endif
    }

    private static string GetEnvironmentName()
        => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

    private static string GetLogDirectory()
        => Environment.GetEnvironmentVariable("HUTAO_LOG_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "logs");

    private static void RegisterExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception,
                "Unhandled exception (IsTerminating={IsTerminating})",
                args.IsTerminating);
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }
}