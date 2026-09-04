using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Serilog;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// 历史趋势视图模型
    /// </summary>
    public class TrendViewModel : DisposableViewModel
    {
        private readonly HistoricalTrendService _trendService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<TrendSeries> _series = new();
        private ObservableCollection<TrendTagItem> _availableTags = new();
        private List<TrendTagItem> _selectedTags = new();

        private DateTime _startTime = DateTime.Now.AddHours(-1);
        private DateTime _endTime = DateTime.Now;
        private int _maxPoints = 1000;
        private AggregationType _aggregation = AggregationType.Raw;
        private TimeSpan _interval = TimeSpan.FromMinutes(1);

        private bool _isLoading;
        private bool _autoRefresh;
        private int _refreshIntervalSeconds = 30;
        private string _statusMessage = "就绪";

        private readonly DispatcherTimer _autoRefreshTimer;

        #endregion

        #region 属性

        public ObservableCollection<TrendSeries> Series => _series;
        public ObservableCollection<TrendTagItem> AvailableTags => _availableTags;
        public List<TrendTagItem> SelectedTags => _selectedTags;

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public int MaxPoints
        {
            get => _maxPoints;
            set => SetProperty(ref _maxPoints, value);
        }

        public AggregationType Aggregation
        {
            get => _aggregation;
            set => SetProperty(ref _aggregation, value);
        }

        public TimeSpan Interval
        {
            get => _interval;
            set => SetProperty(ref _interval, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (SetProperty(ref _autoRefresh, value))
                {
                    if (value) _autoRefreshTimer.Start();
                    else _autoRefreshTimer.Stop();
                }
            }
        }

        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set => SetProperty(ref _refreshIntervalSeconds, value, onChanged: () => _autoRefreshTimer.Interval = TimeSpan.FromSeconds(value));
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<AggregationType> AggregationTypes { get; } = new(Enum.GetValues(typeof(AggregationType)).Cast<AggregationType>());
        public ObservableCollection<TimeSpan> PresetIntervals { get; } = new()
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1)
        };

        #endregion

        #region 命令

        public ICommand QueryCommand { get; }
        public ICommand QuickRangeCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand ClearTagsCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ToggleAutoRefreshCommand { get; }
        public ICommand LoadTagsCommand { get; }

        #endregion

        public TrendViewModel(HistoricalTrendService trendService)
        {
            _trendService = trendService;
            _logger = LogManager.CreateModuleLogger(nameof(TrendViewModel));

            // 初始化命令
            QueryCommand = new AsyncRelayCommand(QueryTrendAsync);
            QuickRangeCommand = new RelayCommand<TimeSpan>(SetQuickRange);
            AddTagCommand = new RelayCommand<TrendTagItem>(AddTag);
            RemoveTagCommand = new RelayCommand<TrendTagItem>(RemoveTag);
            ClearTagsCommand = new RelayCommand(ClearTags);
            ExportDataCommand = new AsyncRelayCommand(ExportDataAsync);
            ToggleAutoRefreshCommand = new RelayCommand(() => AutoRefresh = !AutoRefresh);
            LoadTagsCommand = new AsyncRelayCommand(LoadAvailableTagsAsync);

            // 自动刷新定时器
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds)
            };
            _autoRefreshTimer.Tick += (s, e) => _ = QueryTrendAsync();

            // 加载默认标签
            _ = LoadAvailableTagsAsync();
        }

        private async Task LoadAvailableTagsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载标签列表...";

                // TODO: 从 PLC 或配置加载可用标签
                var mockTags = new[]
                {
                    new TrendTagItem { TagName = "Mixer.Temperature", DisplayName = "混炼机温度", Unit = "℃", Color = "#FF5722" },
                    new TrendTagItem { TagName = "Mixer.Pressure", DisplayName = "混炼机压力", Unit = "MPa", Color = "#2196F3" },
                    new TrendTagItem { TagName = "Mixer.MotorCurrent", DisplayName = "混炼机电流", Unit = "A", Color = "#4CAF50" },
                    new TrendTagItem { TagName = "Tank_Carbon.Level", DisplayName = "炭黑罐液位", Unit = "%", Color = "#FF9800" },
                    new TrendTagItem { TagName = "Tank_Oil.Level", DisplayName = "油罐液位", Unit = "%", Color = "#9C27B0" },
                    new TrendTagItem { TagName = "Blower.Pressure", DisplayName = "风机压力", Unit = "kPa", Color = "#607D8B" },
                    new TrendTagItem { TagName = "Scale_Carbon.Weight", DisplayName = "炭黑秤重量", Unit = "kg", Color = "#795548" },
                    new TrendTagItem { TagName = "Scale_Oil.Weight", DisplayName = "油秤重量", Unit = "kg", Color = "#3F51B5" },
                };

                _availableTags.Clear();
                foreach (var tag in mockTags) _availableTags.Add(tag);

                StatusMessage = $"已加载 {_availableTags.Count} 个标签";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载标签列表失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task QueryTrendAsync()
        {
            if (!_selectedTags.Any())
            {
                StatusMessage = "请先选择要查询的标签";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在查询趋势数据...";

                var param = new TrendQueryParams
                {
                    TagNames = _selectedTags.Select(t => t.TagName).ToList(),
                    StartTime = StartTime,
                    EndTime = EndTime,
                    MaxPoints = MaxPoints,
                    Aggregation = Aggregation,
                    Interval = Aggregation != AggregationType.Raw ? Interval : null
                };

                var series = await _trendService.GetTrendAsync(param);

                _series.Clear();
                foreach (var s in series)
                {
                    var tagInfo = _availableTags.FirstOrDefault(t => t.TagName == s.TagName);
                    if (tagInfo != null)
                    {
                        s.DisplayName = tagInfo.DisplayName;
                        s.Unit = tagInfo.Unit;
                        s.Color = tagInfo.Color;
                    }
                    _series.Add(s);
                }

                StatusMessage = $"查询完成，共 {_series.Count} 条曲线，{_series.Sum(s => s.Points.Count)} 个数据点";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "查询趋势失败");
                StatusMessage = $"查询失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetQuickRange(TimeSpan range)
        {
            EndTime = DateTime.Now;
            StartTime = DateTime.Now - range;
        }

        private void AddTag(TrendTagItem? tag)
        {
            if (tag == null || _selectedTags.Contains(tag)) return;
            _selectedTags.Add(tag);
            OnPropertyChanged(nameof(SelectedTags));
        }

        private void RemoveTag(TrendTagItem? tag)
        {
            if (tag == null) return;
            _selectedTags.Remove(tag);
            OnPropertyChanged(nameof(SelectedTags));
        }

        private void ClearTags()
        {
            _selectedTags.Clear();
            OnPropertyChanged(nameof(SelectedTags));
        }

        private async Task ExportDataAsync()
        {
            if (!_series.Any()) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json",
                FileName = $"TrendData_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var csv = GenerateCsv();
                    System.IO.File.WriteAllText(dialog.FileName, csv);
                    StatusMessage = $"数据已导出: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出数据失败");
                    StatusMessage = $"导出失败: {ex.Message}";
                }
            }
        }

        private string GenerateCsv()
        {
            var lines = new List<string>();
            // 表头
            var header = "Time";
            foreach (var s in _series)
                header += $",{s.DisplayName} ({s.Unit})";
            lines.Add(header);

            // 数据点 - 需要对齐时间戳
            var allPoints = _series.SelectMany(s => s.Points.Select(p => new { s.TagName, p.Time, p.Value })).ToList();
            var timeGroups = allPoints.GroupBy(p => p.Time).OrderBy(g => g.Key);

            foreach (var group in timeGroups)
            {
                var line = group.Key.ToString("yyyy-MM-dd HH:mm:ss.fff");
                foreach (var s in _series)
                {
                    var point = group.FirstOrDefault(p => p.TagName == s.TagName);
                    line += $",{point?.Value ?? 0:F3}";
                }
                lines.Add(line);
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// 趋势标签项
    /// </summary>
    public class TrendTagItem : INotifyPropertyChanged
    {
        private string _tagName = "";
        private string _displayName = "";
        private string _unit = "";
        private string _color = "#2196F3";
        private bool _isSelected;

        public string TagName
        {
            get => _tagName;
            set { _tagName = value; OnPropertyChanged(nameof(TagName)); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}