using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Serilog;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// OEE 分析视图模型
    /// </summary>
    public class OeeAnalysisViewModel : DisposableViewModel
    {
        private readonly OeeAnalysisService _oeeService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<OeeResult> _oeeResults = new();
        private ObservableCollection<DowntimeRecord> _downtimeRecords = new();
        private OeeResult? _selectedOee;

        private string _equipmentCode = "";
        private string _equipmentName = "";
        private DateTime _periodStart = DateTime.Today;
        private DateTime _periodEnd = DateTime.Now;
        private OeePeriod _period = OeePeriod.Shift;

        private decimal _plannedTime = 480; // 分钟
        private decimal _idealRunRate = 1000; // kg/h
        private decimal _actualOutput;
        private decimal _goodOutput;
        private decimal _defectOutput;

        private bool _isCalculating;
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<OeeResult> OeeResults => _oeeResults;
        public ObservableCollection<DowntimeRecord> DowntimeRecords => _downtimeRecords;

        public OeeResult? SelectedOee
        {
            get => _selectedOee;
            set => SetProperty(ref _selectedOee, value, onChanged: OnOeeSelected);
        }

        public string EquipmentCode
        {
            get => _equipmentCode;
            set => SetProperty(ref _equipmentCode, value);
        }

        public string EquipmentName
        {
            get => _equipmentName;
            set => SetProperty(ref _equipmentName, value);
        }

        public DateTime PeriodStart
        {
            get => _periodStart;
            set => SetProperty(ref _periodStart, value);
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set => SetProperty(ref _periodEnd, value);
        }

        public OeePeriod Period
        {
            get => _period;
            set => SetProperty(ref _period, value);
        }

        public decimal PlannedTime
        {
            get => _plannedTime;
            set => SetProperty(ref _plannedTime, value);
        }

        public decimal IdealRunRate
        {
            get => _idealRunRate;
            set => SetProperty(ref _idealRunRate, value);
        }

        public decimal ActualOutput
        {
            get => _actualOutput;
            set => SetProperty(ref _actualOutput, value);
        }

        public decimal GoodOutput
        {
            get => _goodOutput;
            set => SetProperty(ref _goodOutput, value);
        }

        public decimal DefectOutput
        {
            get => _defectOutput;
            set => SetProperty(ref _defectOutput, value);
        }

        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
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

        public ObservableCollection<OeePeriod> PeriodTypes { get; } = new(Enum.GetValues(typeof(OeePeriod)).Cast<OeePeriod>());

        // 计算属性
        public decimal CalculatedAvailability => _selectedOee?.Availability ?? 0;
        public decimal CalculatedPerformance => _selectedOee?.Performance ?? 0;
        public decimal CalculatedQuality => _selectedOee?.Quality ?? 0;
        public decimal CalculatedOee => _selectedOee?.Oee ?? 0;

        #endregion

        #region 命令

        public ICommand CalculateOeeCommand { get; }
        public ICommand LoadHistoryCommand { get; }
        public ICommand LoadTrendCommand { get; }
        public ICommand LoadDowntimeCommand { get; }
        public ICommand ExportOeeCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        #endregion

        public OeeAnalysisViewModel(OeeAnalysisService oeeService)
        {
            _oeeService = oeeService;
            _logger = LogManager.CreateModuleLogger(nameof(OeeAnalysisViewModel));

            CalculateOeeCommand = new AsyncRelayCommand(CalculateOeeAsync);
            LoadHistoryCommand = new AsyncRelayCommand(LoadHistoryAsync);
            LoadTrendCommand = new AsyncRelayCommand(LoadTrendAsync);
            LoadDowntimeCommand = new AsyncRelayCommand(LoadDowntimeAsync);
            ExportOeeCommand = new AsyncRelayCommand(ExportOeeAsync);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => SelectedOee != null);

            // 设置默认设备
            EquipmentCode = "Mixer_01";
            EquipmentName = "主混炼机";
        }

        private async Task CalculateOeeAsync()
        {
            if (string.IsNullOrWhiteSpace(EquipmentCode))
            {
                MessageBox.Show("请输入设备编码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsCalculating = true;
                StatusMessage = "正在计算 OEE...";

                var param = new OeeCalculationParams
                {
                    EquipmentCode = EquipmentCode,
                    EquipmentName = EquipmentName,
                    PlannedProductionTime = PlannedTime,
                    IdealRunRate = IdealRunRate,
                    ActualOutput = ActualOutput,
                    GoodOutput = GoodOutput,
                    DefectOutput = DefectOutput
                };

                var result = await _oeeService.CalculateOeeAsync(EquipmentCode, PeriodStart, PeriodEnd, param);

                StatusMessage = $"OEE 计算完成: OEE={result.Oee:F2}%, 可用率={result.Availability:F2}%, 性能={result.Performance:F2}%, 质量={result.Quality:F2}%";

                // 刷新列表
                await LoadHistoryAsync();

                // 显示结果详情
                MessageBox.Show(
                    $"OEE 计算结果:\n" +
                    $"设备: {result.EquipmentName} ({result.EquipmentCode})\n" +
                    $"周期: {result.PeriodStart:yyyy-MM-dd HH:mm} - {result.PeriodEnd:yyyy-MM-dd HH:mm}\n" +
                    $"OEE: {result.Oee:F2}%\n" +
                    $"可用率: {result.Availability:F2}%\n" +
                    $"性能率: {result.Performance:F2}%\n" +
                    $"质量率: {result.Quality:F2}%\n" +
                    $"计划时间: {result.PlannedProductionTime:F1} 分钟\n" +
                    $"实际运行: {result.ActualRunTime:F1} 分钟\n" +
                    $"停机时间: {result.Downtime:F1} 分钟\n" +
                    $"理想产出: {result.IdealRunRate:F1} kg/h\n" +
                    $"实际产出: {result.ActualOutput:F1} kg\n" +
                    $"合格产出: {result.GoodOutput:F1} kg\n" +
                    $"次品产出: {result.DefectOutput:F1} kg",
                    "OEE 计算结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "计算 OEE 失败");
                StatusMessage = $"计算失败: {ex.Message}";
                MessageBox.Show($"计算失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsCalculating = false;
            }
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载 OEE 历史...";

                var filter = new OeeFilter
                {
                    EquipmentCodes = string.IsNullOrWhiteSpace(EquipmentCode) ? new List<string>() : new List<string> { EquipmentCode },
                    PeriodFrom = PeriodStart,
                    PeriodTo = PeriodEnd,
                    Period = Period,
                    PageIndex = 1,
                    PageSize = 100
                };

                var (items, total) = await _oeeService.GetOeePagedAsync(1, 100, filter);
                _oeeResults.Clear();
                foreach (var r in items) _oeeResults.Add(r);

                StatusMessage = $"OEE 历史: {_oeeResults.Count} 条记录";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载 OEE 历史失败");
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTrendAsync()
        {
            if (string.IsNullOrWhiteSpace(EquipmentCode)) return;

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载 OEE 趋势...";

                var trend = await _oeeService.GetOeeTrendAsync(EquipmentCode, PeriodStart, PeriodEnd, Period);

                // TODO: 显示趋势图表
                StatusMessage = $"OEE 趋势: {trend.Count()} 个数据点";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载 OEE 趋势失败");
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDowntimeAsync()
        {
            if (SelectedOee == null)
            {
                MessageBox.Show("请先选择一条 OEE 记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载停机记录...";

                var records = await _oeeService.GetDowntimeRecordsAsync(EquipmentCode, PeriodStart, PeriodEnd);
                _downtimeRecords.Clear();
                foreach (var r in records) _downtimeRecords.Add(r);

                StatusMessage = $"停机记录: {_downtimeRecords.Count} 条";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载停机记录失败");
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnOeeSelected()
        {
            OnPropertyChanged(nameof(CalculatedAvailability));
            OnPropertyChanged(nameof(CalculatedPerformance));
            OnPropertyChanged(nameof(CalculatedQuality));
            OnPropertyChanged(nameof(CalculatedOee));
        }

        private async Task ExportOeeAsync()
        {
            if (SelectedOee == null)
            {
                MessageBox.Show("请先选择一条 OEE 记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json",
                FileName = $"OEE_{EquipmentCode}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var csv = GenerateOeeCsv(SelectedOee);
                    System.IO.File.WriteAllText(dialog.FileName, csv);
                    StatusMessage = $"OEE 数据已导出: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出 OEE 失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateOeeCsv(OeeResult oee)
        {
            var lines = new List<string>
            {
                "项目,数值",
                $"设备编码,{oee.EquipmentCode}",
                $"设备名称,{oee.EquipmentName}",
                $"周期开始,{oee.PeriodStart:yyyy-MM-dd HH:mm}",
                $"周期结束,{oee.PeriodEnd:yyyy-MM-dd HH:mm}",
                $"OEE (%),{oee.Oee:F2}",
                $"可用率 (%),{oee.Availability:F2}",
                $"性能率 (%),{oee.Performance:F2}",
                $"质量率 (%),{oee.Quality:F2}",
                $"计划生产时间 (分钟),{oee.PlannedProductionTime:F1}",
                $"实际运行时间 (分钟),{oee.ActualRunTime:F1}",
                $"停机时间 (分钟),{oee.Downtime:F1}",
                $"理想生产速率 (kg/h),{oee.IdealRunRate:F1}",
                $"实际产出 (kg),{oee.ActualOutput:F1}",
                $"合格产出 (kg),{oee.GoodOutput:F1}",
                $"次品产出 (kg),{oee.DefectOutput:F1}"
            };

            if (oee.DowntimeRecords.Any())
            {
                lines.Add("");
                lines.Add("停机记录");
                lines.Add("开始时间,结束时间,时长(分钟),原因,类别");
                foreach (var d in oee.DowntimeRecords)
                {
                    lines.Add($"{d.StartTime:yyyy-MM-dd HH:mm},{d.EndTime:yyyy-MM-dd HH:mm},{d.Duration?.TotalMinutes:F1},{d.Reason},{d.Category}");
                }
            }

            if (oee.LossCategories.Any())
            {
                lines.Add("");
                lines.Add("损失分类");
                lines.Add("类别,时间损失(分钟),占比(%)");
                foreach (var l in oee.LossCategories)
                {
                    lines.Add($"{l.Category},{l.TimeLoss:F1},{l.Percentage:F2}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void ViewDetails()
        {
            if (SelectedOee == null) return;
            // TODO: 打开详情窗口
            MessageBox.Show("详情查看功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}