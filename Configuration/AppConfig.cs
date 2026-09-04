using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TwoD5DMonitoring.Infrastructure.Database;

namespace TwoD5DMonitoring.Configuration
{
    public class DatabaseConfig
    {
        public string Provider { get; set; } = "SqlServer";
        public string ConnectionString { get; set; } = "";
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetry { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        
        public Dictionary<string, string> AdditionalSettings { get; set; } = new();
    }

    public class AppConfig
    {
        public DatabaseConfig Database { get; set; } = new();
        public PlcConfig Plc { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public GeneralConfig General { get; set; } = new();
    }

    public class PlcConfig
    {
        public string DefaultDriver { get; set; } = "OpcUa";
        public List<PlcConnectionConfig> Connections { get; set; } = new();
        public int DefaultTimeoutMs { get; set; } = 5000;
        public int ReconnectIntervalMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 10;
    }

    public class PlcConnectionConfig
    {
        public string Name { get; set; } = "";
        public string DriverType { get; set; } = "OpcUa";
        public string EndpointUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CertificatePath { get; set; } = "";
        public bool UseSecurity { get; set; } = false;
        public int PublishingIntervalMs { get; set; } = 1000;
        public Dictionary<string, string> NodeMappings { get; set; } = new();
    }

    public class LoggingConfig
    {
        public string LogDirectory { get; set; } = "Logs";
        public string MinimumLevel { get; set; } = "Information";
        public int RetainedFileCountLimit { get; set; } = 30;
        public bool EnableConsole { get; set; } = true;
        public bool EnableDebug { get; set; } = true;
    }

    public class GeneralConfig
    {
        public string ApplicationName { get; set; } = "TwoD5DMonitoring";
        public string Version { get; set; } = "1.0.0";
        public bool AutoStartRuntime { get; set; } = false;
        public int UiUpdateIntervalMs { get; set; } = 500;
        public bool Enable3DMonitoring { get; set; } = false;
    }

    public static class ConfigManager
    {
        private static IConfigurationRoot? _configuration;
        private static AppConfig? _appConfig;
        private static readonly object _lock = new();

        public static AppConfig Current => _appConfig ??= Load();

        public static AppConfig Load(string configPath = "appsettings.json")
        {
            lock (_lock)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile(configPath, optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                _appConfig = _configuration.Get<AppConfig>() ?? new AppConfig();
                return _appConfig;
            }
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _configuration?.Reload();
                _appConfig = _configuration.Get<AppConfig>() ?? new AppConfig();
            }
        }

        public static T? GetSection<T>(string sectionName) where T : class, new()
        {
            var section = _configuration?.GetSection(sectionName);
            return section?.Get<T>() ?? new T();
        }
    }
}