using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Infrastructure.PLC
{
    /// <summary>
    /// 模拟 PLC 驱动，用于开发和测试
    /// </summary>
    public class MockPlcDriver : IPlcDriver
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, PlcTag> _tags = new();
        private readonly Random _random = new();
        private PlcConnectionState _state = PlcConnectionState.Disconnected;
        private string? _lastError;
        private readonly Timer? _simulationTimer;

        public string Name => "MockPLC";
        public string DriverType => "Mock";
        public PlcConnectionState ConnectionState => _state;
        public string? LastError => _lastError;
        public DateTime? LastSuccessfulRead { get; private set; }

        public event EventHandler<PlcValueChangedEventArgs>? ValueChanged;
        public event EventHandler<PlcConnectionEventArgs>? ConnectionStateChanged;

        public MockPlcDriver()
        {
            _logger = LogManager.CreateModuleLogger(nameof(MockPlcDriver));
            InitializeDefaultTags();
            _simulationTimer = new Timer(SimulateTagChanges, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void InitializeDefaultTags()
        {
            var defaultTags = new[]
            {
                new PlcTag { Name = "Temperature", Address = "Temp", DataType = PlcDataType.Real, Value = 25.0 },
                new PlcTag { Name = "Pressure", Address = "Press", DataType = PlcDataType.Real, Value = 1.5 },
                new PlcTag { Name = "MotorRunning", Address = "Motor", DataType = PlcDataType.Bool, Value = false },
                new PlcTag { Name = "ValveState", Address = "Valve", DataType = PlcDataType.Bool, Value = false },
                new PlcTag { Name = "TankLevel", Address = "Level", DataType = PlcDataType.Real, Value = 50.0 },
                new PlcTag { Name = "FlowRate", Address = "Flow", DataType = PlcDataType.Real, Value = 100.0 },
                new PlcTag { Name = "RotorSpeed", Address = "Speed", DataType = PlcDataType.DInt, Value = 1500 },
                new PlcTag { Name = "Weight", Address = "Weight", DataType = PlcDataType.Real, Value = 0.0 },
            };

            foreach (var tag in defaultTags)
            {
                _tags[tag.Name] = tag;
            }
        }

        public Task<bool> ConnectAsync(PlcConnectionConfig config)
        {
            var oldState = _state;
            _state = PlcConnectionState.Connected;
            _lastError = null;

            ConnectionStateChanged?.Invoke(this, new PlcConnectionEventArgs
            {
                PlcName = Name,
                OldState = oldState,
                NewState = _state
            });

            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            var oldState = _state;
            _state = PlcConnectionState.Disconnected;

            ConnectionStateChanged?.Invoke(this, new PlcConnectionEventArgs
            {
                PlcName = Name,
                OldState = oldState,
                NewState = _state
            });

            return Task.CompletedTask;
        }

        public Task<bool> ReconnectAsync()
        {
            return ConnectAsync(new PlcConnectionConfig());
        }

        public Task<T?> ReadAsync<T>(string tagName)
        {
            var value = ReadAsync(tagName, GetDataTypeForTag(tagName)).Result;
            if (value == null) return Task.FromResult(default(T));

            try
            {
                return Task.FromResult((T)Convert.ChangeType(value, typeof(T)));
            }
            catch
            {
                return Task.FromResult(default(T));
            }
        }

        public async Task<object?> ReadAsync(string tagName, PlcDataType dataType)
        {
            await Task.Delay(1);

            if (_state != PlcConnectionState.Connected)
            {
                if (_tags.TryGetValue(tagName, out var cached))
                    return Task.FromResult(cached.Value);
                return Task.FromResult<object?>(null);
            }

            if (_tags.TryGetValue(tagName, out var tag))
            {
                var newValue = SimulateValueChange(tag);
                tag.Value = newValue;
                tag.Timestamp = DateTime.Now;
                tag.Quality = true;

                LastSuccessfulRead = DateTime.Now;

                return Task.FromResult<object?>(newValue);
            }

            return Task.FromResult<object?>(null);
        }

        public Task<Dictionary<string, object?>> ReadMultipleAsync(IEnumerable<string> tagNames)
        {
            var result = new Dictionary<string, object?>();
            foreach (var tagName in tagNames)
            {
                result[tagName] = ReadAsync(tagName, GetDataTypeForTag(tagName)).Result;
            }
            return Task.FromResult(result);
        }

        public async Task<bool> WriteAsync(string tagName, object value)
        {
            await Task.Delay(1);

            if (_state != PlcConnectionState.Connected)
            {
                _lastError = "Not connected";
                return false;
            }

            if (_tags.TryGetValue(tagName, out var tag))
            {
                var oldValue = tag.Value;
                tag.Value = value;
                tag.Timestamp = DateTime.Now;
                tag.Quality = true;

                ValueChanged?.Invoke(this, new PlcValueChangedEventArgs
                {
                    TagName = tagName,
                    OldValue = oldValue,
                    NewValue = value,
                    Timestamp = DateTime.Now,
                    Quality = true
                });

                return true;
            }

            _lastError = $"Tag not found: {tagName}";
            return false;
        }

        public Task<bool> WriteMultipleAsync(Dictionary<string, object> values)
        {
            var tasks = values.Select(kvp => WriteAsync(kvp.Key, kvp.Value));
            var results = Task.WhenAll(tasks).Result;
            return Task.FromResult(results.All(r => r));
        }

        public Task<IEnumerable<PlcTag>> BrowseTagsAsync(string? nodePath = null)
        {
            return Task.FromResult(_tags.Values.AsEnumerable());
        }

        public Task<PlcTag?> GetTagInfoAsync(string tagName)
        {
            _tags.TryGetValue(tagName, out var tag);
            return Task.FromResult(tag);
        }

        public Task<bool> SubscribeAsync(IEnumerable<string> tagNames, int publishingIntervalMs = 1000)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UnsubscribeAsync(IEnumerable<string> tagNames)
        {
            return Task.FromResult(true);
        }

        public bool IsTagCached(string tagName) => _tags.ContainsKey(tagName);

        public object? GetCachedValue(string tagName) => _tags.TryGetValue(tagName, out var tag) ? tag.Value : null;

        public void SetCachedValue(string tagName, object value, bool quality = true)
        {
            _tags.AddOrUpdate(tagName,
                new PlcTag { Name = tagName, Value = value, Timestamp = DateTime.Now, Quality = quality },
                (key, existing) => { existing.Value = value; existing.Timestamp = DateTime.Now; existing.Quality = quality; return existing; });
        }

        public void Dispose()
        {
            _simulationTimer?.Dispose();
            _ = DisconnectAsync();
        }

        private void SimulateTagChanges(object? state)
        {
            foreach (var tag in _tags.Values)
            {
                var newValue = SimulateValueChange(tag);
                if (!Equals(tag.Value, newValue))
                {
                    var oldValue = tag.Value;
                    tag.Value = newValue;
                    tag.Timestamp = DateTime.Now;

                    ValueChanged?.Invoke(this, new PlcValueChangedEventArgs
                    {
                        TagName = tag.Name,
                        OldValue = oldValue,
                        NewValue = newValue,
                        Timestamp = DateTime.Now,
                        Quality = true
                    });
                }
            }
        }

        private object SimulateValueChange(PlcTag tag)
        {
            return tag.DataType switch
            {
                PlcDataType.Real => Math.Round((double)tag.Value! + new Random().NextDouble() * 2 - 1, 2),
                PlcDataType.DInt => (int)tag.Value! + new Random().Next(-10, 11),
                PlcDataType.Bool => new Random().NextDouble() > 0.99 ? !(bool)tag.Value! : tag.Value,
                PlcDataType.Int => (short)tag.Value! + new Random().Next(-5, 6),
                _ => tag.Value
            };
        }

        private PlcDataType GetDataTypeForTag(string tagName)
        {
            if (_tags.TryGetValue(tagName, out var tag))
                return tag.DataType;

            var lower = tagName.ToLowerInvariant();
            if (lower.Contains("temp") || lower.Contains("press") || lower.Contains("flow") ||
                lower.Contains("level") || lower.Contains("speed") || lower.Contains("weight"))
                return PlcDataType.Real;
            if (lower.Contains("motor") || lower.Contains("valve") || lower.Contains("state") || lower.Contains("run"))
                return PlcDataType.Bool;
            if (lower.Contains("count") || lower.Contains("total") || lower.Contains("speed"))
                return PlcDataType.DInt;

            return PlcDataType.Real;
        }
    }
}