using System.Reflection;

using ChatHubClient;

using ChattyApp.ViewModels;

using Microsoft.Extensions.Configuration;

using Serilog;

namespace ChattyApp
{
    public static class MauiProgram
    {
        private static IConfiguration s_config;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            AddConfiguration(builder); // This sets: s_config

            AddLogger(builder);

            builder.Services.AddSingleton<ChatHubService>(sp =>
                new ChatHubService(s_config
                    .GetSection("SignalR:HubUrl")
                    .Value!));

            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }

        private static MauiAppBuilder AddLogger(MauiAppBuilder builder)
        {
            builder.Services.AddLogging(logger =>
                logger.AddSerilog(Log.Logger = new LoggerConfiguration()
                    .WriteTo.Seq(
                        serverUrl: s_config.GetSection("Telemetry:LoggingUrl").Value!,
                        apiKey: s_config.GetSection("Telemetry:LoggingApiKey").Value!)
                    .CreateLogger()));

            return builder;
        }

        private static MauiAppBuilder AddConfiguration(MauiAppBuilder builder)
        {
            const string configFileName = "ChattyApp.Resources.appsettings.json";

            using var configStream = Assembly.GetExecutingAssembly()
                                         .GetManifestResourceStream(configFileName)
                                     ?? throw new ArgumentException($"Configuration file ({configFileName}) not found!", nameof(configFileName));

            s_config = new ConfigurationBuilder()
                .AddJsonStream(configStream)
                .Build();

            builder.Configuration.AddConfiguration(s_config);

            return builder;
        }
    }
}