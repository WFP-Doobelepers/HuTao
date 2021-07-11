using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Zhongli.Data;
using Zhongli.Data.Config;
using Zhongli.Services.AutoRemoveMessage;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Image;
using Zhongli.Services.Quote;

namespace Zhongli.Bot
{
    public class Bot
    {
        private const bool AttemptReset = true;
        private static CancellationTokenSource? _mediatorToken;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
        private CancellationTokenSource _reconnectCts = null!;

        public static async Task Main() => await new Bot().StartAsync();

        private static ServiceProvider ConfigureServices() =>
            new ServiceCollection().AddHttpClient().AddMemoryCache()
                .AddDbContext<ZhongliContext>(ContextOptions, ServiceLifetime.Transient)
                .AddMediatR(c => c.Using<ZhongliMediator>(),
                    typeof(Bot), typeof(ZhongliMediator))
                .AddLogging(l => l.AddSerilog())
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandErrorHandler>()
                .AddSingleton<CommandHandlingService>()
                .AddTransient<AuthorizationService>()
                .AddSingleton<ModerationService>()
                .AddScoped<InteractiveService>().AddImages()
                .AddCommandHelp()
                .AddAutoRemoveMessage()
                .AddSingleton<IQuoteService, QuoteService>()
                .BuildServiceProvider();

        private static void ContextOptions(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ZhongliContext>()
                .Build();

            optionsBuilder.UseLazyLoadingProxies();

#if DEBUG
            optionsBuilder.UseNpgsql(configuration.GetSection(nameof(ZhongliConfig.Debug))[nameof(BotConfig.ZhongliContext)]);
#else
            optionsBuilder.UseNpgsql(configuration.GetSection(nameof(ZhongliConfig.Release))[nameof(BotConfig.ZhongliContext)]));
#endif
        }

        private async Task StartAsync()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<ZhongliConfig>()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            await using var services = ConfigureServices();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            var commands = services.GetRequiredService<CommandService>();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var mediator = services.GetRequiredService<IMediator>();

            _reconnectCts  = new CancellationTokenSource();
            _mediatorToken = new CancellationTokenSource();

            await new DiscordSocketListener(client, mediator)
                .StartAsync(_mediatorToken.Token);

            client.Disconnected += _ => ClientOnDisconnected(client);
            client.Connected    += ClientOnConnected;

            client.Log   += LogAsync;
            commands.Log += LogAsync;
#if DEBUG
            await client.LoginAsync(TokenType.Bot, config.GetSection(nameof(ZhongliConfig.Debug)).GetValue<string>(nameof(BotConfig.Token)));
#else
            await client.LoginAsync(TokenType.Bot, config.GetSection(nameof(ZhongliConfig.Release)).GetValue<string>(nameof(BotConfig.Token)));
#endif
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task ClientOnConnected()
        {
            Log.Debug("Client reconnected, resetting cancel tokens...");

            _reconnectCts.Cancel();
            _reconnectCts = new CancellationTokenSource();

            Log.Debug("Client reconnected, cancel tokens reset.");
            return Task.CompletedTask;
        }

        private Task ClientOnDisconnected(IDiscordClient client)
        {
            // Check the state after <timeout> to see if we reconnected
            Log.Information("Client disconnected, starting timeout task...");
            _ = Task.Delay(Timeout, _reconnectCts.Token).ContinueWith(async _ =>
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

            Log.Write(severity, message.Exception, message.Message);

            return Task.CompletedTask;
        }

        private void FailFast()
            => Environment.Exit(1);

        private async Task CheckStateAsync(IDiscordClient client)
        {
            // Client reconnected, no need to reset
            if (client.ConnectionState == ConnectionState.Connected) return;

            if (AttemptReset)
            {
                Log.Information("Attempting to reset the client");

                var timeout = Task.Delay(Timeout);
                var connect = client.StartAsync();
                var task = await Task.WhenAny(timeout, connect);

                if (task == timeout)
                {
                    Log.Fatal("Client reset timed out (task deadlocked?), killing process");
                    FailFast();
                }
                else if (connect.IsFaulted)
                {
                    Log.Fatal("Client reset faulted, killing process", connect.Exception);
                    FailFast();
                }
                else if (connect.IsCompletedSuccessfully)
                {
                    Log.Information("Client reset succesfully!");
                }

                return;
            }

            Log.Fatal("Client did not reconnect in time, killing process");
            FailFast();
        }
    }
}