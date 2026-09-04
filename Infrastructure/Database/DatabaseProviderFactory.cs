using System;
using TwoD5DMonitoring.Configuration;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public static class DatabaseProviderFactory
    {
        private static IDatabaseProvider? _provider;
        private static readonly object _lock = new();

        public static IDatabaseProvider CreateProvider(string providerName, string connectionString)
        {
            return providerName.ToLowerInvariant() switch
            {
                "mysql" => new MySqlProvider(),
                "postgresql" or "postgres" => new PostgreSqlProvider(),
                "sqlserver" or "mssql" or "sql" => new SqlServerProvider(),
                _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported")
            };
        }

        public static IDatabaseProvider GetProvider()
        {
            lock (_lock)
            {
                if (_provider != null)
                    return _provider;

                var config = ConfigManager.Current.Database;
                _provider = CreateProvider(config.Provider, config.ConnectionString);
                
                LogManager.LogOperation("Database Provider Created", "Provider: {Provider}, ConnectionString: {ConnStr}", true, 
                    config.Provider, MaskConnectionString(config.ConnectionString));
                
                return _provider;
            }
        }

        public static void SetProvider(IDatabaseProvider provider)
        {
            lock (_lock)
            {
                _provider?.Dispose();
                _provider = provider;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _provider?.Dispose();
                _provider = null;
            }
        }

        private static string MaskConnectionString(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                var masked = new System.Collections.Generic.List<string>();
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 && (kv[0].Trim().ToLower().Contains("password") || kv[0].Trim().ToLower().Contains("pwd")))
                    {
                        masked.Add($"{kv[0]}=***");
                    }
                    else
                    {
                        masked.Add(part);
                    }
                }
                return string.Join(";", masked);
            }
            catch
            {
                return "***";
            }
        }
    }
}