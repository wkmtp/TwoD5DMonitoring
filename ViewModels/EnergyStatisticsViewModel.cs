using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Serilog;
using TwoD5DMonitoring.Extensions;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// 能耗统计视图模型
    /// </summary>
    public class EnergyStatisticsViewModel : DisposableViewModel
    {
        private readonly EnergyStatisticsService _energyService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<EnergyStatistics> _cachedStatistics = new();
        private ObservableCollection<EnergyRecord> _energyRecords = new();
        private ObservableCollection<string> _availableMeters = new();

        private EnergyType _energyTypeFilter = EnergyType.Electricity;
        private DateTime _periodStart = DateTime.Today.AddDays(-7);
        private DateTime _periodEnd = DateTime.Today.AddDays(1).AddSeconds(-1);
        private StatisticsPeriod _statisticsPeriod = StatisticsPeriod.Day;
        private string _meterFilter = "";
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<EnergyStatistics> CachedStatistics => _cachedStatistics;
        public ObservableCollection<EnergyRecord> EnergyRecords => _energyRecords;
        public ObservableCollection<string> AvailableMeters => _availableMeters;

        public EnergyType EnergyTypeFilter
        {
            get => _energyTypeFilter;
            set => SetProperty(ref _energyTypeFilter, value, onChanged: () => _ = LoadCachedStatisticsAsync());
        }

        public DateTime PeriodStart
        {
            get => _periodStart;
            set => SetProperty(ref _periodStart, value, onChanged: () => _ = LoadCachedStatisticsAsync());
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set => SetProperty(ref _periodEnd, value, onChanged: () => _ = LoadCachedStatisticsAsync());
        }

        public StatisticsPeriod StatisticsPeriod
        {
            get => _statisticsPeriod;
            set => SetProperty(ref _statisticsPeriod, value, onChanged: () => _ = LoadCachedStatisticsAsync());
        }

        public string MeterFilter
        {
            get => _meterFilter;
            set => SetProperty(ref _meterFilter, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 命令

        public ICommand LoadStatisticsCommand { get; }
        public ICommand QueryStatisticsCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportChartCommand { get; }
        public ICommand RefreshMetersCommand { get; }
        public ICommand ToggleAutoRefreshCommand { get; }

        #endregion

        public EnergyStatisticsViewModel(EnergyStatisticsService energyService)
        {
            _energyService = energyService;
            _logger = LogManager.CreateModuleLogger(nameof(EnergyStatisticsViewModel));

            LoadStatisticsCommand = new AsyncRelayCommand(LoadCachedStatisticsAsync);
            QueryStatisticsCommand = new AsyncRelayCommand(QueryStatisticsAsync);
            ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
            ExportChartCommand = new AsyncRelayCommand(ExportChartAsync);
            RefreshMetersCommand = new AsyncRelayCommand(LoadAvailableMetersAsync);
            ToggleAutoRefreshCommand = new AsyncRelayCommand(ToggleAutoRefreshAsync);

            _ = LoadAvailableMetersAsync();
            _ = LoadCachedStatisticsAsync();
        }

        private async Task LoadCachedStatisticsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载缓存统计...";

                var stats = await _energyService.GetCachedStatisticsAsync(
                    EnergyTypeFilter, 
                    string.IsNullOrEmpty(MeterFilter) ? null : MeterFilter, 
                    PeriodStart, 
                    PeriodEnd, 
                    StatisticsPeriod);

                _cachedStatistics.Clear();
                foreach (var s in stats.OrderByDescending(s => s.PeriodStart))
                    _cachedStatistics.Add(s);

                StatusMessage = $"就绪 - {_cachedStatistics.Count} 条统计记录";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载缓存统计失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task QueryStatisticsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在查询统计数据...";

                var stats = await _energyService.GetStatisticsAsync(new EnergyQueryParams
                {
                    EnergyType = EnergyTypeFilter,
                    MeterCodes = string.IsNullOrWhiteSpace(MeterFilter) ? new List<string>() : new List<string> { MeterFilter },
                    StartTime = PeriodStart,
                    EndTime = PeriodEnd,
                    Period = StatisticsPeriod
                });

                _energyRecords.Clear();
                foreach (var r in stats.HourlyData.OrderBy(d => d.Key))
                {
                    _energyRecords.Add(new EnergyRecord
                    {
                        MeterCode = stats.MeterCode,
                        MeterName = stats.MeterName,
                        EnergyType = EnergyTypeFilter,
                        Timestamp = r.Key,
                        Consumption = r.Value
                    });
                }

                StatusMessage = $"查询完成 - {_energyRecords.Count} 条记录";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "查询统计失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportCsvAsync()
        {
            if (_cachedStatistics.Count == 0)
            {
                MessageBox.Show("没有可导出的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                FileName = $"EnergyStatistics_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var stats = _cachedStatistics.FirstOrDefault();
                    if (stats == null) return;

                    var csv = GenerateStatisticsCsv(stats);
                    System.IO.File.WriteAllText(dialog.FileName, csv);
                    MessageBox.Show("导出成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出 CSV 失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportChartAsync()
        {
            MessageBox.Show("图表导出功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task LoadAvailableMetersAsync()
        {
            _availableMeters.Clear();
            var mockMeters = new[]
            {
                "Meter_Electricity_Main",
                "Meter_Electricity_Mixer",
                "Meter_Electricity_Blower",
                "Meter_Gas_Boiler",
                "Meter_Water_Main",
                "Meter_Steam_Boiler",
                "Meter_Air_Compressor"
            };
            foreach (var m in mockMeters) _availableMeters.Add(m);
        }

        private string GenerateStatisticsCsv(EnergyStatistics stats)
        {
            var lines = new List<string>
            {
                "项目,数值",
                $"能源类型,{stats.EnergyType}",
                $"统计周期,{stats.PeriodStart:yyyy-MM-dd HH:mm} - {stats.PeriodEnd:yyyy-MM-dd HH:mm}",
                $"总消耗,{stats.TotalConsumption:F2} {stats.EnergyType.GetUnit()}",
                $"总费用,{stats.TotalCost:F2} 元",
                $"平均每小时消耗,{stats.AvgConsumptionPerHour:F2} {stats.EnergyType.GetUnit()}/h",
                $"峰值消耗,{stats.PeakConsumption:F2} {stats.EnergyType.GetUnit()}",
                $"峰值时间,{stats.PeakTime:yyyy-MM-dd HH:mm}",
                $"最小消耗,{stats.MinConsumption:F2} {stats.EnergyType.GetUnit()}",
                $"最小时间,{stats.MinTime:yyyy-MM-dd HH:mm}",
                $"同比变化 (%),{stats.YoYChangePercent:F2}",
                $"环比变化 (%),{stats.MoMChangePercent:F2}",
                $"吨产品能耗,{stats.ConsumptionPerTon:F2} {stats.EnergyType.GetUnit()}/吨",
                $"批次能耗,{stats.ConsumptionPerBatch:F2} {stats.EnergyType.GetUnit()}/批"
            };

            if (stats.ByEquipment.Any())
            {
                lines.Add("");
                lines.Add("分设备消耗");
                lines.Add("设备,消耗量,占比(%)");
                var total = stats.ByEquipment.Values.Sum();
                foreach (var kvp in stats.ByEquipment.OrderByDescending(k => k.Value))
                {
                    lines.Add($"{kvp.Key},{kvp.Value:F2},{(total > 0 ? kvp.Value / total * 100 : 0):F2}");
                }
            }

            if (stats.HourlyData.Any())
            {
                lines.Add("");
                lines.Add("小时消耗趋势");
                lines.Add("时间,消耗量");
                foreach (var kvp in stats.HourlyData.OrderBy(k => k.Key))
                {
                    lines.Add($"{kvp.Key:yyyy-MM-dd HH:00},{kvp.Value:F2}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private async Task ToggleAutoRefreshAsync()
        {
            // TODO: 实现自动刷新切换
            MessageBox.Show("自动刷新功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}