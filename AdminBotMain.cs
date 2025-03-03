using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Services.ComponentInteractions;
using NetCord.Gateway;
using Supabase;
using StackExchange.Redis;
using dotenv.net;
using AdminBot.Services;
using System.Reflection;
using AdminBot.Services.Background;
using NetCord.Hosting.Services.ComponentInteractions;
using AdminBot.Modules;
using AdminBot.Models;

namespace AdminBot;

/// <summary>
/// Main entry point for the Discord bot application following NetCord patterns.
/// </summary>
public class AdminBotMain
{
    private static ILoggerFactory? _loggerFactory;
    private static LoggingService? _loggingService;
    public static readonly CancellationTokenSource GlobalProgramCancel = new();

    /// <summary>
    /// Entry point configured according to NetCord's hosting model.
    /// </summary>
    public static async Task Main(string[] args)
    {
        try
        {
            // Initialize early logging
            ConfigureEarlyLogging();

            // Load environment configuration
            LoadEnvironmentVariables();

            // Create the host builder
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

            // Configure services
            hostBuilder.ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            });

            // Build and configure the host
            IHost host = hostBuilder.Build();

            // Configure interactions and event handlers
            ConfigureHost(host);

            // Start logging service
            _loggingService?.Info("Bot starting up...");

            // Run the host
            await host.RunAsync(GlobalProgramCancel.Token);
        }
        catch (Exception ex)
        {
            _loggingService?.Critical("Host terminated unexpectedly", ex);
            throw;
        }
        finally
        {
            if (_loggingService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            GlobalProgramCancel.Dispose();
        }
    }

    /// <summary>
    /// Configures services following NetCord's DI patterns.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configure NetCord Gateway
        services.AddDiscordGateway(options =>
        {
            options.Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new InvalidOperationException("Discord token not found");

            options.Intents = GatewayIntents.GuildMessages
                            | GatewayIntents.DirectMessages
                            | GatewayIntents.MessageContent
                            | GatewayIntents.DirectMessageReactions
                            | GatewayIntents.GuildMessageReactions
                            | GatewayIntents.GuildVoiceStates;
        });

        // Add NetCord's component interactions
        services.AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
                .AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>()
                .AddComponentInteractions<UserMenuInteraction, UserMenuInteractionContext>()
                .AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>()
                .AddComponentInteractions<MentionableMenuInteraction, MentionableMenuInteractionContext>()
                .AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>()
                .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

        // Add gateway event handlers
        services.AddGatewayEventHandlers(Assembly.GetExecutingAssembly());

        // Configure base services
        ConfigureBaseServices(services);

        // Configure bot services
        ConfigureBotServices(services);
    }

    /// <summary>
    /// Configures base infrastructure services.
    /// </summary>
    private static void ConfigureBaseServices(IServiceCollection services)
    {
        // Configure logging
        services.AddSingleton<LoggingService>();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new LoggingServiceProvider(_loggingService!));
        });

        // Configure Supabase
        services.AddSingleton<Client>(provider =>
        {
            string? url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            string? key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Supabase configuration missing");
            }

            return new Client(url, key, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            });
        });

        // Configure Redis
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            string connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
            string password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            
            ConfigurationOptions config = ConfigurationOptions.Parse(connectionString);
            if (!string.IsNullOrEmpty(password))
            {
                config.Password = password;
            }
            
            return ConnectionMultiplexer.Connect(config);
        });

        // Add cache service
        services.AddSingleton<ICacheService, CacheService>();
    }

    /// <summary>
    /// Configures bot-specific services.
    /// </summary>
    private static void ConfigureBotServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<DatabaseService>();
        
        // Register BotGatewayClient as a wrapper around GatewayClient
        services.AddSingleton<BotGatewayClient>(provider => {
            var logger = provider.GetRequiredService<ILogger<BotGatewayClient>>();
            var token = provider.GetRequiredService<IEntityToken>();
            var configuration = provider.GetRequiredService<GatewayClientConfiguration>();
            return new BotGatewayClient(logger, token, configuration);
        });

        // Feature services
        services.AddSingleton<VoiceService>();
        services.AddSingleton<XPCommands>();
        services.AddSingleton<Warning>();

        // Background services
        services.AddHostedService<CacheMaintenanceService>();
        services.AddHostedService<MetricsCollectionService>();
    }

    /// <summary>
    /// Configures the host after build.
    /// </summary>
    private static void ConfigureHost(IHost host)
    {
        // Configure gateway event handlers
        host.UseGatewayEventHandlers();

        // Add modules
        host.AddModules(typeof(AdminBotMain).Assembly);
    }

    /// <summary>
    /// Configures early logging before DI is available.
    /// </summary>
    private static void ConfigureEarlyLogging()
    {
        // Create logger factory
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Create logging service
        _loggingService = new LoggingService(
            _loggerFactory.CreateLogger<LoggingService>(),
            null!, // TODO: These will be properly injected later
            null!,
            null!);

        // Start file logging
        _loggingService.Info("Initializing logging system...");
    }

    /// <summary>Loads and validates environment variables.</summary>
    private static void LoadEnvironmentVariables()
    {
        string envPath = Path.Combine(AppContext.BaseDirectory, ".env");
        if (File.Exists(envPath))
        {
            DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));
        }

        string[] requiredVars = ["DISCORD_TOKEN", "SUPABASE_URL", "SUPABASE_KEY"];
        var missing = requiredVars.Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)));

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing required environment variables: {string.Join(", ", missing)}");
        }
    }

    /// <summary>Custom logging provider that uses our LoggingService.</summary>
    private class LoggingServiceProvider : ILoggerProvider
    {
        private readonly LoggingService _loggingService;

        public LoggingServiceProvider(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggingService;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}