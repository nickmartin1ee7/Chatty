﻿using System.Reflection;

using ChatHubClient;

using ChattyApp.ViewModels;

using Microsoft.Extensions.Configuration;

using Serilog;

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

            builder.Services.AddTransient<HttpClient>();

            builder.Services.AddSingleton<ChatHubService>(sp =>
                new ChatHubService(
                    sp.GetRequiredService<HttpClient>(),
                    _settings.SignalR.HubUrl));

            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }

        private static MauiAppBuilder AddLogger(MauiAppBuilder builder)
        {
            builder.Logging.AddSerilog(Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Seq(
                    serverUrl: _settings.Telemetry.LoggingUrl,
                    apiKey: _settings.Telemetry.LoggingApiKey)
                .Enrich.WithProperty("DeviceId", _settings.Telemetry.DeviceId)
                .CreateLogger());

            return builder;
        }

        private static MauiAppBuilder AddConfiguration(MauiAppBuilder builder, string? deviceId)
        {
            const string configFileName = "ChattyApp.Resources.appsettings.json";

            using var configStream = Assembly.GetExecutingAssembly()
                                         .GetManifestResourceStream(configFileName)
                                     ?? throw new ArgumentException($"Configuration file ({configFileName}) not found!", nameof(configFileName));

            IConfiguration config;
            builder.Configuration.AddConfiguration(config = new ConfigurationBuilder()
                .AddJsonStream(configStream)
                .Build());

            _settings = config.Get<Settings>() ?? throw new ArgumentNullException(nameof(Settings), "Failed to create settings from appsettings file");
            _settings.Telemetry.DeviceId = deviceId;

            builder.Services.AddTransient<Settings>(_ => _settings);

            return builder;
        }
    }
}