using System.Diagnostics;
using System.Reflection;

using ChatHubClient;

using ChattyApp.ViewModels;

using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Debugging;

namespace ChattyApp
{
    public static class MauiProgram
    {
        private static Settings _settings;

        public static MauiApp CreateMauiApp(string deviceId = null)
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            AddConfiguration(builder, deviceId); // This sets: s_config

            AddLogger(builder);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            builder.Services.AddTransient<HttpClient>();

            builder.Services.AddSingleton<ChatHubService>(sp =>
                new ChatHubService(
                    sp.GetRequiredService<HttpClient>(),
                    _settings.SignalR.HubUrl));

            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            Log.Logger.Fatal(exception,
                "[CRASH] The application crashed due to an unhandled exception ({exceptionType}): {exceptionMessage}",
                exception.GetType().Name,
                exception.Message);

            Log.CloseAndFlush();
        }

        private static MauiAppBuilder AddLogger(MauiAppBuilder builder)
        {
            SelfLog.Enable(msg =>
                Debug.WriteLine(msg));

            builder.Logging.AddSerilog(Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.Seq(
                    serverUrl: _settings.Telemetry.LoggingUrl,
                    apiKey: _settings.Telemetry.LoggingApiKey)
                .Enrich.WithProperty("DeviceId", _settings.Telemetry.DeviceId)
                .CreateLogger());

            return builder;
        }

        private static MauiAppBuilder AddConfiguration(MauiAppBuilder builder, string deviceId)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var configFileName = $"{assembly.GetName().Name}.Resources.appsettings.json";

            using var configStream = assembly
                                         .GetManifestResourceStream(configFileName)
                                     ?? throw new ArgumentException($"Configuration file ({configFileName}) not found!", nameof(configFileName));

            var configBuilder = new ConfigurationBuilder()
                .AddJsonStream(configStream);

#if DEBUG
            var secretsFileName = $"{assembly.GetName().Name}.Resources.secrets.json";
            using var secretsStream = assembly.GetManifestResourceStream(secretsFileName);
            if (secretsStream is not null)
                configBuilder.AddJsonStream(secretsStream);
#endif

            var config = configBuilder.Build();
            builder.Configuration.AddConfiguration(config);

            _settings = config.Get<Settings>() ?? throw new ArgumentNullException(nameof(Settings), "Failed to create settings from appsettings file");
            _settings.Telemetry.DeviceId = deviceId;

            builder.Services.AddTransient<Settings>(_ => _settings);

            return builder;
        }
    }
}