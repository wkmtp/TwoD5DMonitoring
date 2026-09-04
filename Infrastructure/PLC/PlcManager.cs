using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using TwoD5DMonitoring.Configuration;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Infrastructure.PLC
{
    public class PlcManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IPlcDriver> _drivers = new();
        private readonly ConcurrentDictionary<string, PlcConnectionConfig> _configs = new();
        private readonly Timer? _healthCheckTimer;
        private bool _disposed;

        public event EventHandler<PlcValueChangedEventArgs>? AnyValueChanged;
        public event EventHandler<PlcConnectionEventArgs>? AnyConnectionStateChanged;

        public IReadOnlyDictionary<string, IPlcDriver> Drivers => _drivers;

        public PlcManager()
        {
            _logger = LogManager.CreateModuleLogger(nameof(PlcManager));
            _healthCheckTimer = new Timer(HealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void Initialize()
        {
            var plcConfig = ConfigManager.Current.Plc;
            
            var hasConnections = plcConfig.Connections?.Any() == true;
            
            if (hasConnections)
            {
                foreach (var connConfig in plcConfig.Connections)
                {
                    _configs[connConfig.Name] = new PlcConnectionConfig
                    {
                        Name = connConfig.Name,
                        DriverType = connConfig.DriverType,
                        EndpointUrl = connConfig.EndpointUrl,
                        Username = connConfig.Username,
                        Password = connConfig.Password,
                        CertificatePath = connConfig.CertificatePath,
                        UseSecurity = connConfig.UseSecurity,
                        PublishingIntervalMs = connConfig.PublishingIntervalMs,
                        NodeMappings = connConfig.NodeMappings
                    };
                    var driver = CreateDriver(connConfig.DriverType);
                    if (driver != null)
                    {
                        driver.ValueChanged += OnDriverValueChanged;
                        driver.ConnectionStateChanged += OnDriverConnectionStateChanged;
                        _drivers[connConfig.Name] = driver;
                    }
                }
            }
            else
            {
                // 如果没有配置连接，创建一个模拟驱动用于测试
                var mockDriver = new MockPlcDriver();
                mockDriver.ValueChanged += OnDriverValueChanged;
                mockDriver.ConnectionStateChanged += OnDriverConnectionStateChanged;
                _drivers["MockPLC"] = mockDriver;
            }

            LogManager.LogOperation("PLC Manager Initialized", $"Driver Count: {_drivers.Count}");
        }

        public IPlcDriver? GetDriver(string name)
        {
            _drivers.TryGetValue(name, out var driver);
            return driver;
        }

        public IPlcDriver? GetDriverByType(string driverType)
        {
            return _drivers.Values.FirstOrDefault(d => d.DriverType == driverType);
        }

        public async Task<bool> ConnectAllAsync()
        {
            var tasks = new List<Task<bool>>();
            
            foreach (var kvp in _drivers)
            {
                if (_configs.TryGetValue(kvp.Key, out var config))
                {
                    tasks.Add(ConnectDriverAsync(kvp.Key, config));
                }
            }

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        public async Task<bool> ConnectAsync(string name)
        {
            if (_drivers.TryGetValue(name, out var driver) && _configs.TryGetValue(name, out var config))
            {
                return await ConnectDriverAsync(name, config);
            }
            return false;
        }

        private async Task<bool> ConnectDriverAsync(string name, PlcConnectionConfig config)
        {
            if (!_drivers.TryGetValue(name, out var driver))
                return false;

            var result = await driver.ConnectAsync(config);
            
            if (result)
            {
                // 自动订阅配置的标签
                if (config.NodeMappings.Count > 0)
                {
                    await driver.SubscribeAsync(config.NodeMappings.Keys, config.PublishingIntervalMs);
                }
            }

            return result;
        }

        public async Task DisconnectAllAsync()
        {
            var tasks = _drivers.Values.Select(d => d.DisconnectAsync());
            await Task.WhenAll(tasks);
        }

        public async Task DisconnectAsync(string name)
        {
            if (_drivers.TryGetValue(name, out var driver))
            {
                await driver.DisconnectAsync();
            }
        }

        public async Task<T?> ReadAsync<T>(string plcName, string tagName)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.ReadAsync<T>(tagName);
            }
            return default;
        }

        public async Task<object?> ReadAsync(string plcName, string tagName, PlcDataType dataType)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.ReadAsync(tagName, dataType);
            }
            return null;
        }

        public async Task<Dictionary<string, object?>> ReadMultipleAsync(string plcName, IEnumerable<string> tagNames)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.ReadMultipleAsync(tagNames);
            }
            return new Dictionary<string, object?>();
        }

        public async Task<bool> WriteAsync(string plcName, string tagName, object value)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.WriteAsync(tagName, value);
            }
            return false;
        }

        public async Task<bool> WriteMultipleAsync(string plcName, Dictionary<string, object> values)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.WriteMultipleAsync(values);
            }
            return false;
        }

        public async Task<bool> SubscribeAsync(string plcName, IEnumerable<string> tagNames, int publishingIntervalMs = 1000)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.SubscribeAsync(tagNames, publishingIntervalMs);
            }
            return false;
        }

        public async Task<bool> UnsubscribeAsync(string plcName, IEnumerable<string> tagNames)
        {
            if (_drivers.TryGetValue(plcName, out var driver))
            {
                return await driver.UnsubscribeAsync(tagNames);
            }
            return false;
        }

        public bool IsConnected(string plcName)
        {
            return _drivers.TryGetValue(plcName, out var driver) && driver.ConnectionState == PlcConnectionState.Connected;
        }

        public PlcConnectionState GetConnectionState(string plcName)
        {
            return _drivers.TryGetValue(plcName, out var driver) ? driver.ConnectionState : PlcConnectionState.Disconnected;
        }

        public Dictionary<string, PlcConnectionState> GetAllConnectionStates()
        {
            return _drivers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ConnectionState);
        }

        private void OnDriverValueChanged(object? sender, PlcValueChangedEventArgs e)
        {
            AnyValueChanged?.Invoke(this, e);
        }

        private void OnDriverConnectionStateChanged(object? sender, PlcConnectionEventArgs e)
        {
            AnyConnectionStateChanged?.Invoke(this, e);
        }

        private void HealthCheck(object? state)
        {
            if (_disposed) return;

            foreach (var kvp in _drivers)
            {
                var driver = kvp.Value;
                var config = _configs.TryGetValue(kvp.Key, out var c) ? c : null;

                if (driver.ConnectionState == PlcConnectionState.Error || 
                    driver.ConnectionState == PlcConnectionState.Disconnected)
                {
                    if (ConfigManager.Current.Plc.MaxReconnectAttempts > 0)
                    {
                        _ = Task.Run(async () =>
                        {
                            LogManager.LogOperation("PLC Health Check Reconnect", $"Driver: {kvp.Key}");
                            await driver.ReconnectAsync();
                        });
                    }
                }
                else if (driver.ConnectionState == PlcConnectionState.Connected)
                {
                    // 检查最后成功读取时间
                    if (driver.LastSuccessfulRead.HasValue)
                    {
                        var elapsed = DateTime.Now - driver.LastSuccessfulRead.Value;
                        if (elapsed > TimeSpan.FromMinutes(2))
                        {
                            LogManager.LogOperation("PLC Stale Connection", $"Driver: {kvp.Key}, LastRead: {elapsed.TotalSeconds}s ago");
                        }
                    }
                }
            }
        }

        private IPlcDriver? CreateDriver(string driverType)
        {
            return driverType.ToLowerInvariant() switch
            {
                // "opcua_siemens" or "siemens" => new SiemensDriver(),
                // "opcua_omron" or "omron" => new OmronDriver(),
                // "opcua_mitsubishi" or "mitsubishi" => new MitsubishiDriver(),
                // "opcua" => new SiemensDriver(), // 默认
                "mock" => new MockPlcDriver(),
                _ => new MockPlcDriver() // 默认使用模拟驱动
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                _healthCheckTimer?.Dispose();
                _ = DisconnectAllAsync();
                
                foreach (var driver in _drivers.Values)
                {
                    driver.ValueChanged -= OnDriverValueChanged;
                    driver.ConnectionStateChanged -= OnDriverConnectionStateChanged;
                    driver.Dispose();
                }
                _drivers.Clear();
                _configs.Clear();
            }
        }
    }
}