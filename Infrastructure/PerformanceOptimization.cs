using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Infrastructure
{
    /// <summary>
    /// 性能监控与优化工具类
    /// </summary>
    public static class PerformanceOptimizer
    {
        private static readonly ILogger _logger = LogManager.CreateModuleLogger(nameof(PerformanceOptimizer));
        private static readonly ConcurrentDictionary<string, PerformanceCounter> _counters = new();
        private static readonly Timer _reportTimer;
        private static long _uiUpdateCount;
        private static long _renderCount;
        private static readonly Stopwatch _uptime = Stopwatch.StartNew();

        static PerformanceOptimizer()
        {
            _reportTimer = new Timer(ReportPerformance, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// 记录UI更新性能
        /// </summary>
        public static void RecordUIUpdate(string operationName, TimeSpan elapsed)
        {
            Interlocked.Increment(ref _uiUpdateCount);
            
            var counter = _counters.GetOrAdd(operationName, _ => new PerformanceCounter(operationName));
            counter.Record(elapsed);
            
            // 超过阈值记录警告
            if (elapsed.TotalMilliseconds > 16.67) // 60 FPS = 16.67ms
            {
                _logger.Warning("UI更新耗时过长: {Operation} = {ElapsedMs:F2}ms", operationName, elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 记录渲染性能
        /// </summary>
        public static void RecordRender(TimeSpan elapsed)
        {
            Interlocked.Increment(ref _renderCount);
            
            var counter = _counters.GetOrAdd("Render", _ => new PerformanceCounter("Render"));
            counter.Record(elapsed);
        }

        /// <summary>
        /// 测量并记录操作耗时
        /// </summary>
        public static T Measure<T>(string operationName, Func<T> func)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return func();
            }
            finally
            {
                sw.Stop();
                RecordOperation(operationName, sw.Elapsed);
            }
        }

        /// <summary>
        /// 测量并记录异步操作耗时
        /// </summary>
        public static async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> func)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return await func();
            }
            finally
            {
                sw.Stop();
                RecordOperation(operationName, sw.Elapsed);
            }
        }

        /// <summary>
        /// 测量无返回值操作
        /// </summary>
        public static void Measure(string operationName, Action action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                sw.Stop();
                RecordOperation(operationName, sw.Elapsed);
            }
        }

        /// <summary>
        /// 测量异步无返回值操作
        /// </summary>
        public static async Task MeasureAsync(string operationName, Func<Task> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                sw.Stop();
                RecordOperation(operationName, sw.Elapsed);
            }
        }

        private static void RecordOperation(string operationName, TimeSpan elapsed)
        {
            var counter = _counters.GetOrAdd(operationName, _ => new PerformanceCounter(operationName));
            counter.Record(elapsed);
        }

        private static void ReportPerformance(object? state)
        {
            var uptime = _uptime.Elapsed;
            var uiUpdates = Interlocked.Read(ref _uiUpdateCount);
            var renders = Interlocked.Read(ref _renderCount);
            
            _logger.Information("=== 性能报告 (运行时间: {Uptime}) ===", uptime.ToString(@"hh\:mm\:ss"));
            _logger.Information("UI更新总数: {Count}, 平均频率: {Freq:F1}/min", uiUpdates, uiUpdates / Math.Max(1, uptime.TotalMinutes));
            _logger.Information("渲染总数: {Count}, 平均频率: {Freq:F1}/min", renders, renders / Math.Max(1, uptime.TotalMinutes));

            foreach (var counter in _counters.Values.OrderByDescending(c => c.AverageMs))
            {
                if (counter.Count > 0)
                {
                    _logger.Information("  {Name}: Count={Count}, Avg={Avg:F2}ms, Max={Max:F2}ms, P95={P95:F2}ms",
                        counter.Name, counter.Count, counter.AverageMs, counter.MaxMs, counter.Percentile95);
                }
            }
        }

        /// <summary>
        /// 获取性能摘要
        /// </summary>
        public static PerformanceSummary GetSummary()
        {
            return new PerformanceSummary
            {
                Uptime = _uptime.Elapsed,
                TotalUIUpdates = Interlocked.Read(ref _uiUpdateCount),
                TotalRenders = Interlocked.Read(ref _renderCount),
                Counters = _counters.Values.Select(c => c.GetSnapshot()).ToList()
            };
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        public static void Reset()
        {
            foreach (var counter in _counters.Values)
            {
                counter.Reset();
            }
            Interlocked.Exchange(ref _uiUpdateCount, 0);
            Interlocked.Exchange(ref _renderCount, 0);
            _uptime.Restart();
        }
    }

    /// <summary>
    /// 性能计数器
    /// </summary>
    public class PerformanceCounter
    {
        private readonly string _name;
        private readonly object _lock = new();
        private long _count;
        private double _totalMs;
        private double _maxMs;
        private readonly List<double> _recentSamples = new();
        private const int MaxSamples = 1000;

        public PerformanceCounter(string name)
        {
            _name = name;
        }

        public string Name => _name;
        public long Count => _count;
        public double AverageMs => _count > 0 ? _totalMs / _count : 0;
        public double MaxMs => _maxMs;

        public double Percentile95
        {
            get
            {
                lock (_lock)
                {
                    if (_recentSamples.Count == 0) return 0;
                    var sorted = _recentSamples.OrderBy(x => x).ToList();
                    var index = (int)Math.Ceiling(sorted.Count * 0.95) - 1;
                    return sorted[Math.Max(0, index)];
                }
            }
        }

        public void Record(TimeSpan elapsed)
        {
            var ms = elapsed.TotalMilliseconds;
            lock (_lock)
            {
                _count++;
                _totalMs += ms;
                _maxMs = Math.Max(_maxMs, ms);
                _recentSamples.Add(ms);
                if (_recentSamples.Count > MaxSamples)
                    _recentSamples.RemoveAt(0);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _count = 0;
                _totalMs = 0;
                _maxMs = 0;
                _recentSamples.Clear();
            }
        }

        public CounterSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new CounterSnapshot
                {
                    Name = _name,
                    Count = _count,
                    AverageMs = AverageMs,
                    MaxMs = _maxMs,
                    Percentile95 = Percentile95
                };
            }
        }
    }

    /// <summary>
    /// 性能计数器快照
    /// </summary>
    public class CounterSnapshot
    {
        public string Name { get; set; } = "";
        public long Count { get; set; }
        public double AverageMs { get; set; }
        public double MaxMs { get; set; }
        public double Percentile95 { get; set; }
    }

    /// <summary>
    /// 性能摘要
    /// </summary>
    public class PerformanceSummary
    {
        public TimeSpan Uptime { get; set; }
        public long TotalUIUpdates { get; set; }
        public long TotalRenders { get; set; }
        public List<CounterSnapshot> Counters { get; set; } = new();
    }

    /// <summary>
    /// UI 虚拟化助手
    /// </summary>
    public static class VirtualizationHelper
    {
        /// <summary>
        /// 为 ItemsControl 启用虚拟化
        /// </summary>
        public static void EnableVirtualization(System.Windows.Controls.ItemsControl itemsControl, double itemHeight = 30)
        {
            if (itemsControl == null) return;

            itemsControl.ItemsPanel = new System.Windows.Controls.ItemsPanelTemplate(
                new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.VirtualizingStackPanel)));

            // 设置虚拟化相关的附加属性
            System.Windows.Controls.VirtualizingPanel.SetIsVirtualizing(itemsControl, true);
            System.Windows.Controls.VirtualizingPanel.SetVirtualizationMode(itemsControl, VirtualizationMode.Recycling);

            // 设置容器样式以复用
            var containerStyle = new Style(typeof(System.Windows.Controls.ContentPresenter));
            containerStyle.Setters.Add(new Setter(System.Windows.Controls.VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling));
            itemsControl.ItemContainerStyle = containerStyle;
        }

        /// <summary>
        /// 创建虚拟化列表的数据模板选择器
        /// </summary>
        public static DataTemplateSelector CreateTemplateSelector<T>(Dictionary<Type, DataTemplate> templates)
        {
            return new GenericTemplateSelector<T>(templates);
        }
    }

    /// <summary>
    /// 通用数据模板选择器
    /// </summary>
    public class GenericTemplateSelector<T> : DataTemplateSelector
    {
        private readonly Dictionary<Type, DataTemplate> _templates = new();

        public GenericTemplateSelector(Dictionary<Type, DataTemplate> templates)
        {
            foreach (var kvp in templates)
            {
                _templates[kvp.Key] = kvp.Value;
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return base.SelectTemplate(item, container);

            var type = item.GetType();
            while (type != null)
            {
                if (_templates.TryGetValue(type, out var template))
                    return template;
                type = type.BaseType;
            }
            return base.SelectTemplate(item, container);
        }
    }

    /// <summary>
    /// 防抖动/节流工具
    /// </summary>
    public class DebounceThrottle
    {
        private readonly TimeSpan _interval;
        private readonly Action _action;
        private readonly ILogger _logger;
        private Timer? _timer;
        private readonly object _lock = new();
        private bool _pending;

        public DebounceThrottle(TimeSpan interval, Action action)
        {
            _interval = interval;
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _logger = LogManager.CreateModuleLogger(nameof(DebounceThrottle));
        }

        /// <summary>
        /// 防抖动: 只有在间隔内无新调用时才执行
        /// </summary>
        public void Debounce()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = new Timer(_ => Execute(), null, _interval, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// 节流: 固定间隔执行，忽略间隔内的多余调用
        /// </summary>
        public void Throttle()
        {
            lock (_lock)
            {
                if (_pending) return;
                _pending = true;
                _timer?.Dispose();
                _timer = new Timer(_ =>
                {
                    lock (_lock)
                    {
                        _pending = false;
                    }
                    Execute();
                }, null, _interval, Timeout.InfiniteTimeSpan);
            }
        }

        private void Execute()
        {
            try
            {
                _action();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DebounceThrottle action failed");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <summary>
    /// 批量更新助手 - 合并短时间内的多次更新
    /// </summary>
    public class BatchUpdater<T>
    {
        private readonly TimeSpan _batchWindow;
        private readonly Action<IReadOnlyList<T>> _batchAction;
        private readonly ILogger _logger;
        private readonly List<T> _pendingItems = new();
        private Timer? _timer;
        private readonly object _lock = new();

        public BatchUpdater(TimeSpan batchWindow, Action<IReadOnlyList<T>> batchAction)
        {
            _batchWindow = batchWindow;
            _batchAction = batchAction ?? throw new ArgumentNullException(nameof(batchAction));
            _logger = LogManager.CreateModuleLogger("BatchUpdater");
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _pendingItems.Add(item);
                if (_timer == null)
                {
                    _timer = new Timer(_ => Flush(), null, _batchWindow, Timeout.InfiniteTimeSpan);
                }
            }
        }

        public void Flush()
        {
            List<T> itemsToProcess;
            lock (_lock)
            {
                if (_pendingItems.Count == 0) return;
                itemsToProcess = _pendingItems.ToList();
                _pendingItems.Clear();
                _timer?.Dispose();
                _timer = null;
            }

            if (itemsToProcess.Count > 0)
            {
                try
                {
                    _batchAction(itemsToProcess);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "BatchUpdater action failed");
                }
            }
        }

        public void Dispose()
        {
            Flush();
            _timer?.Dispose();
        }
    }

    /// <summary>
    /// 图像/几何缓存
    /// </summary>
    public class GeometryCache
    {
        private readonly ConcurrentDictionary<string, Geometry> _cache = new();
        private readonly int _maxSize;
        private readonly object _lock = new();
        private readonly List<string> _accessOrder = new();

        public GeometryCache(int maxSize = 100)
        {
            _maxSize = maxSize;
        }

        public Geometry GetOrCreate(string key, Func<Geometry> factory)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                // LRU: 移动到末尾
                lock (_lock)
                {
                    _accessOrder.Remove(key);
                    _accessOrder.Add(key);
                }
                return cached;
            }

            var geometry = factory();
            geometry.Freeze(); // 重要: 冻结以跨线程访问

            if (_cache.TryAdd(key, geometry))
            {
                lock (_lock)
                {
                    _accessOrder.Add(key);
                    // 超过最大值移除最旧
                    while (_accessOrder.Count > _maxSize)
                    {
                        var oldest = _accessOrder[0];
                        _accessOrder.RemoveAt(0);
                        _cache.TryRemove(oldest, out _);
                    }
                }
            }
            else
            {
                // 并发添加失败，使用现有的
                _cache.TryGetValue(key, out geometry);
            }

            return geometry!;
        }

        public void Clear()
        {
            _cache.Clear();
            lock (_lock) _accessOrder.Clear();
        }

        public int Count => _cache.Count;
    }
}