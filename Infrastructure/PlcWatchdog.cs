using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwoD5DMonitoring.Configuration;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;
using Serilog;

namespace TwoD5DMonitoring.Infrastructure
{
    /// <summary>
    /// PLC 看门狗 - 监控连接健康、自动重连、离线缓存同步
    /// </summary>
    public class PlcWatchdog : IDisposable
    {
        private readonly ILogger _logger;
        private readonly PlcManager _plcManager;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _syncTimer;
        private readonly ConcurrentDictionary<string, PlcHealthInfo> _healthInfo = new();
        private readonly ConcurrentDictionary<string, OfflineBuffer> _offlineBuffers = new();
        private readonly WatchdogConfig _config;
        private bool _disposed;

        public event EventHandler<WatchdogEventArgs>? HealthChanged;
        public event EventHandler<WatchdogEventArgs>? Reconnecting;
        public event EventHandler<WatchdogEventArgs>? Reconnected;
        public event EventHandler<WatchdogEventArgs>? OfflineDataSynced;

        public PlcWatchdog(PlcManager plcManager, WatchdogConfig? config = null)
        {
            _logger = LogManager.CreateModuleLogger(nameof(PlcWatchdog));
            _plcManager = plcManager ?? throw new ArgumentNullException(nameof(plcManager));
            _config = config ?? new WatchdogConfig();

            // 订阅 PLC 管理器事件
            _plcManager.AnyConnectionStateChanged += OnPlcConnectionStateChanged;
            _plcManager.AnyValueChanged += OnPlcValueChanged;

            // 健康检查定时器 (每10秒)
            _healthCheckTimer = new Timer(HealthCheck, null, 
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            // 离线数据同步定时器 (每30秒)
            _syncTimer = new Timer(SyncOfflineData, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.Information("PLC Watchdog started");
        }

        private void OnPlcConnectionStateChanged(object? sender, PlcConnectionEventArgs e)
        {
            var info = _healthInfo.GetOrAdd(e.PlcName, _ => new PlcHealthInfo { PlcName = e.PlcName });
            
            info.LastStateChange = DateTime.Now;
            info.CurrentState = e.NewState;
            info.LastError = e.ErrorMessage;

            if (e.NewState == PlcConnectionState.Connected)
            {
                info.ConsecutiveFailures = 0;
                info.LastSuccessfulConnection = DateTime.Now;
                _logger.Information("PLC {PlcName} connected", e.PlcName);
            }
            else if (e.NewState == PlcConnectionState.Error || e.NewState == PlcConnectionState.Disconnected)
            {
                info.ConsecutiveFailures++;
                _logger.Warning("PLC {PlcName} disconnected/error: {Error}", e.PlcName, e.ErrorMessage);
            }

            HealthChanged?.Invoke(this, new WatchdogEventArgs
            {
                PlcName = e.PlcName,
                EventType = WatchdogEventType.ConnectionStateChanged,
                Message = $"PLC {e.PlcName} state: {e.OldState} -> {e.NewState}",
                Timestamp = DateTime.Now
            });
        }

        private void OnPlcValueChanged(object? sender, PlcValueChangedEventArgs e)
        {
            // 更新最后活跃时间
            // 这里需要通过标签名反向查找 PLC 名称，简化处理
        }

        private void HealthCheck(object? state)
        {
            if (_disposed) return;

            try
            {
                var states = _plcManager.GetAllConnectionStates();
                
                foreach (var kvp in states)
                {
                    var plcName = kvp.Key;
                    var connectionState = kvp.Value;
                    
                    var info = _healthInfo.GetOrAdd(plcName, _ => new PlcHealthInfo { PlcName = plcName });
                    info.CurrentState = connectionState;
                    info.LastHealthCheck = DateTime.Now;

                    // 检查是否需要重连
                    if (ShouldReconnect(plcName, info))
                    {
                        _logger.Warning("PLC {PlcName} health check failed, initiating reconnect (attempt {Attempt})", 
                            plcName, info.ReconnectAttempts + 1);
                        
                        Reconnecting?.Invoke(this, new WatchdogEventArgs
                        {
                            PlcName = plcName,
                            EventType = WatchdogEventType.Reconnecting,
                            Message = $"Initiating reconnect attempt {info.ReconnectAttempts + 1}",
                            Timestamp = DateTime.Now
                        });

                        _ = Task.Run(async () => await AttemptReconnect(plcName));
                    }

                    // 检查数据新鲜度
                    CheckDataFreshness(plcName, info);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Health check error");
            }
        }

        private bool ShouldReconnect(string plcName, PlcHealthInfo info)
        {
            if (info.CurrentState == PlcConnectionState.Connected)
            {
                // 检查最后成功读取时间
                var driver = _plcManager.GetDriver(plcName);
                if (driver?.LastSuccessfulRead.HasValue == true)
                {
                    var stale = DateTime.Now - driver.LastSuccessfulRead.Value;
                    if (stale > _config.MaxDataStaleness)
                    {
                        info.ConsecutiveFailures++;
                        return info.ConsecutiveFailures >= _config.ConsecutiveFailuresBeforeReconnect;
                    }
                }
                return false;
            }

            if (info.CurrentState == PlcConnectionState.Error || info.CurrentState == PlcConnectionState.Disconnected)
            {
                return info.ReconnectAttempts < _config.MaxReconnectAttempts;
            }

            return false;
        }

        private async Task AttemptReconnect(string plcName)
        {
            var info = _healthInfo.GetOrAdd(plcName, _ => new PlcHealthInfo { PlcName = plcName });
            info.ReconnectAttempts++;

            try
            {
                var result = await _plcManager.ConnectAsync(plcName);
                
                if (result)
                {
                    info.ReconnectAttempts = 0;
                    info.LastSuccessfulConnection = DateTime.Now;
                    info.ConsecutiveFailures = 0;
                    
                    _logger.Information("PLC {PlcName} reconnected successfully", plcName);
                    
                    Reconnected?.Invoke(this, new WatchdogEventArgs
                    {
                        PlcName = plcName,
                        EventType = WatchdogEventType.Reconnected,
                        Message = "Reconnected successfully",
                        Timestamp = DateTime.Now
                    });

                    // 同步离线期间的数据
                    await SyncOfflineBuffer(plcName);
                }
                else
                {
                    _logger.Warning("PLC {PlcName} reconnect attempt {Attempt} failed", plcName, info.ReconnectAttempts);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "PLC {PlcName} reconnect error", plcName);
            }
        }

        private void CheckDataFreshness(string plcName, PlcHealthInfo info)
        {
            var driver = _plcManager.GetDriver(plcName);
            if (driver?.LastSuccessfulRead.HasValue == true)
            {
                var stale = DateTime.Now - driver.LastSuccessfulRead.Value;
                info.DataStaleness = stale;
                
                if (stale > _config.MaxDataStaleness)
                {
                    info.IsDataStale = true;
                    _logger.Warning("PLC {PlcName} data stale: {Staleness}", plcName, stale);
                }
                else
                {
                    info.IsDataStale = false;
                }
            }
        }

        private void SyncOfflineData(object? state)
        {
            if (_disposed) return;

            foreach (var plcName in _offlineBuffers.Keys)
            {
                if (_plcManager.IsConnected(plcName))
                {
                    _ = Task.Run(async () => await SyncOfflineBuffer(plcName));
                }
            }
        }

        private async Task SyncOfflineBuffer(string plcName)
        {
            if (!_offlineBuffers.TryGetValue(plcName, out var buffer))
                return;

            var items = buffer.GetAndClear();
            if (items.Count == 0) return;

            try
            {
                _logger.Information("Syncing {Count} offline items for PLC {PlcName}", items.Count, plcName);
                
                var driver = _plcManager.GetDriver(plcName);
                if (driver != null)
                {
                    // 批量写入离线数据
                    foreach (var item in items)
                    {
                        await driver.WriteAsync(item.TagName, item.Value);
                    }
                }

                OfflineDataSynced?.Invoke(this, new WatchdogEventArgs
                {
                    PlcName = plcName,
                    EventType = WatchdogEventType.OfflineDataSynced,
                    Message = $"Synced {items.Count} offline items",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to sync offline data for PLC {PlcName}", plcName);
                // 重新放回缓冲区
                buffer.AddRange(items);
            }
        }

        /// <summary>
        /// 缓存离线写入数据
        /// </summary>
        public void BufferOfflineWrite(string plcName, string tagName, object value)
        {
            var buffer = _offlineBuffers.GetOrAdd(plcName, _ => new OfflineBuffer(_config.OfflineBufferSize));
            buffer.Add(new OfflineDataItem
            {
                TagName = tagName,
                Value = value,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 获取健康状态报告
        /// </summary>
        public WatchdogReport GetReport()
        {
            return new WatchdogReport
            {
                GeneratedAt = DateTime.Now,
                PlcHealth = _healthInfo.Values.Select(h => new PlcHealthSummary
                {
                    PlcName = h.PlcName,
                    CurrentState = h.CurrentState,
                    ConsecutiveFailures = h.ConsecutiveFailures,
                    ReconnectAttempts = h.ReconnectAttempts,
                    LastSuccessfulConnection = h.LastSuccessfulConnection,
                    LastHealthCheck = h.LastHealthCheck,
                    DataStaleness = h.DataStaleness,
                    IsDataStale = h.IsDataStale,
                    OfflineBufferSize = _offlineBuffers.TryGetValue(h.PlcName, out var b) ? b.Count : 0
                }).ToList()
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _healthCheckTimer?.Dispose();
            _syncTimer?.Dispose();

            _plcManager.AnyConnectionStateChanged -= OnPlcConnectionStateChanged;
            _plcManager.AnyValueChanged -= OnPlcValueChanged;

            // 刷新所有离线缓冲区
            foreach (var buffer in _offlineBuffers.Values)
            {
                buffer.Clear();
            }

            _logger.Information("PLC Watchdog stopped");
        }
    }

    /// <summary>
    /// 看门狗配置
    /// </summary>
    public class WatchdogConfig
    {
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan MaxDataStaleness { get; set; } = TimeSpan.FromSeconds(30);
        public int ConsecutiveFailuresBeforeReconnect { get; set; } = 3;
        public int MaxReconnectAttempts { get; set; } = 10;
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5);
        public int OfflineBufferSize { get; set; } = 10000;
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// PLC 健康信息
    /// </summary>
    public class PlcHealthInfo
    {
        public string PlcName { get; set; } = "";
        public PlcConnectionState CurrentState { get; set; }
        public DateTime LastStateChange { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public DateTime LastSuccessfulConnection { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int ReconnectAttempts { get; set; }
        public TimeSpan DataStaleness { get; set; }
        public bool IsDataStale { get; set; }
        public string? LastError { get; set; }
    }

    /// <summary>
    /// 离线数据缓冲区
    /// </summary>
    public class OfflineBuffer
    {
        private readonly Queue<OfflineDataItem> _queue;
        private readonly int _maxSize;
        private readonly object _lock = new();

        public OfflineBuffer(int maxSize)
        {
            _maxSize = maxSize;
            _queue = new Queue<OfflineDataItem>(maxSize);
        }

        public void Add(OfflineDataItem item)
        {
            lock (_lock)
            {
                if (_queue.Count >= _maxSize)
                {
                    _queue.Dequeue(); // 移除最旧
                }
                _queue.Enqueue(item);
            }
        }

        public void AddRange(IEnumerable<OfflineDataItem> items)
        {
            lock (_lock)
            {
                foreach (var item in items)
                {
                    if (_queue.Count >= _maxSize)
                        _queue.Dequeue();
                    _queue.Enqueue(item);
                }
            }
        }

        public List<OfflineDataItem> GetAndClear()
        {
            lock (_lock)
            {
                var list = _queue.ToList();
                _queue.Clear();
                return list;
            }
        }

        public void Clear()
        {
            lock (_lock) _queue.Clear();
        }

        public int Count
        {
            get { lock (_lock) return _queue.Count; }
        }
    }

    /// <summary>
    /// 离线数据项
    /// </summary>
    public class OfflineDataItem
    {
        public string TagName { get; set; } = "";
        public object Value { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 看门狗事件参数
    /// </summary>
    public class WatchdogEventArgs : EventArgs
    {
        public string PlcName { get; set; } = "";
        public WatchdogEventType EventType { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public enum WatchdogEventType
    {
        ConnectionStateChanged,
        Reconnecting,
        Reconnected,
        OfflineDataSynced,
        DataStale
    }

    /// <summary>
    /// 看门狗报告
    /// </summary>
    public class WatchdogReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<PlcHealthSummary> PlcHealth { get; set; } = new();
    }

    public class PlcHealthSummary
    {
        public string PlcName { get; set; } = "";
        public PlcConnectionState CurrentState { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int ReconnectAttempts { get; set; }
        public DateTime LastSuccessfulConnection { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public TimeSpan DataStaleness { get; set; }
        public bool IsDataStale { get; set; }
        public int OfflineBufferSize { get; set; }
    }
}