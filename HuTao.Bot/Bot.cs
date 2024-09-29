using System;
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
using Serilog.Events;

namespace HuTao.Bot;

public class Bot
{
    private static CancellationTokenSource? _mediatorToken;
    private static readonly TimeSpan ResetTimeout = TimeSpan.FromSeconds(15);
    private CancellationTokenSource _reconnectCts = null!;

    public static async Task Main() => await new Bot().StartAsync();

    private static ServiceProvider ConfigureServices() =>
        new ServiceCollection()
            .AddLogging(l => l.AddSerilog())
            .AddHttpClient().AddMemoryCache().AddHangfireServer()
            .AddDbContext<HuTaoContext>(ContextOptions)
            .AddMediatR(c => c.RegisterServicesFromAssemblies(typeof(HuTaoMediator).Assembly, typeof(Bot).Assembly))
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Hangfire", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        await using var services = ConfigureServices();

        GlobalConfiguration.Configuration
            .UseActivator(new AspNetCoreJobActivator(
                services.GetRequiredService<IServiceScopeFactory>()))
            .UseSerilogLogProvider()
            .UsePostgreSqlStorage(HuTaoConfig.Configuration.HangfireContext)
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
        => Environment.Exit(1);
}