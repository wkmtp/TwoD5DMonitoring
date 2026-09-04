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
    /// 生产报表视图模型
    /// </summary>
    public class ProductionReportViewModel : DisposableViewModel
    {
        private readonly ProductionReportService _reportService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<ProductionReport> _reports = new();
        private ObservableCollection<ShiftDefinition> _shifts = new();
        private ProductionReport? _selectedReport;

        private ReportType _reportTypeFilter = ReportType.Daily;
        private ReportStatus? _statusFilter;
        private DateTime _periodFrom = DateTime.Today;
        private DateTime _periodTo = DateTime.Now;
        private int _page = 1;
        private int _pageSize = 20;
        private int _totalCount;

        private bool _isGenerating;
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<ProductionReport> Reports => _reports;
        public ObservableCollection<ShiftDefinition> Shifts => _shifts;

        public ProductionReport? SelectedReport
        {
            get => _selectedReport;
            set => SetProperty(ref _selectedReport, value, onChanged: OnReportSelected);
        }

        public ReportType ReportTypeFilter
        {
            get => _reportTypeFilter;
            set => SetProperty(ref _reportTypeFilter, value, onChanged: () => { _page = 1; _ = LoadReportsAsync(); });
        }

        public ReportStatus? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value, onChanged: () => { _page = 1; _ = LoadReportsAsync(); });
        }

        public DateTime PeriodFrom
        {
            get => _periodFrom;
            set => SetProperty(ref _periodFrom, value, onChanged: () => { _page = 1; _ = LoadReportsAsync(); });
        }

        public DateTime PeriodTo
        {
            get => _periodTo;
            set => SetProperty(ref _periodTo, value, onChanged: () => { _page = 1; _ = LoadReportsAsync(); });
        }

        public int Page
        {
            get => _page;
            set => SetProperty(ref _page, value, onChanged: () => _ = LoadReportsAsync());
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value, onChanged: () => { _page = 1; _ = LoadReportsAsync(); });
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
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

        public ObservableCollection<ReportType> ReportTypes { get; } = new(Enum.GetValues(typeof(ReportType)).Cast<ReportType>());
        public ObservableCollection<ReportStatus> ReportStatuses { get; } = new(Enum.GetValues(typeof(ReportStatus)).Cast<ReportStatus>());

        #endregion

        #region 命令

        public ICommand LoadCommand { get; }
        public ICommand GenerateShiftReportCommand { get; }
        public ICommand GenerateDailyReportCommand { get; }
        public ICommand GenerateMonthlyReportCommand { get; }
        public ICommand ViewReportCommand { get; }
        public ICommand ApproveReportCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand PrintReportCommand { get; }
        public ICommand DeleteReportCommand { get; }

        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        public ProductionReportViewModel(ProductionReportService reportService)
        {
            _reportService = reportService;
            _logger = LogManager.CreateModuleLogger(nameof(ProductionReportViewModel));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            GenerateShiftReportCommand = new AsyncRelayCommand(GenerateShiftReportAsync);
            GenerateDailyReportCommand = new AsyncRelayCommand(GenerateDailyReportAsync);
            GenerateMonthlyReportCommand = new AsyncRelayCommand(GenerateMonthlyReportAsync);
            ViewReportCommand = new RelayCommand(ViewReport, () => SelectedReport != null);
            ApproveReportCommand = new AsyncRelayCommand(ApproveReportAsync, () => SelectedReport?.Status == ReportStatus.Generated);
            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync, () => SelectedReport != null);
            PrintReportCommand = new RelayCommand(PrintReport, () => SelectedReport != null);
            DeleteReportCommand = new AsyncRelayCommand(DeleteReportAsync, () => SelectedReport != null);

            PrevPageCommand = new RelayCommand(() => Page--, () => Page > 1);
            NextPageCommand = new RelayCommand(() => Page++, () => Page < TotalPages);
            FirstPageCommand = new RelayCommand(() => Page = 1, () => Page > 1);
            LastPageCommand = new RelayCommand(() => Page = TotalPages, () => Page < TotalPages);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await LoadShiftsAsync();
            await LoadReportsAsync();
        }

        private async Task LoadShiftsAsync()
        {
            try
            {
                var shifts = await _reportService.GetShiftsAsync();
                _shifts.Clear();
                foreach (var s in shifts) _shifts.Add(s);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载班次失败");
            }
        }

        private async Task LoadReportsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载报表列表...";

                var filter = new ReportFilter
                {
                    ReportType = ReportTypeFilter,
                    Status = StatusFilter,
                    PeriodFrom = PeriodFrom,
                    PeriodTo = PeriodTo,
                    PageIndex = Page,
                    PageSize = PageSize
                };

                var (items, total) = await _reportService.GetReportsPagedAsync(Page, PageSize, filter);
                _reports.Clear();
                foreach (var r in items) _reports.Add(r);
                TotalCount = total;

                OnPropertyChanged(nameof(TotalPages));
                StatusMessage = $"报表: {_reports.Count} / {total} 条";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载报表失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnReportSelected()
        {
            OnPropertyChanged(nameof(SelectedReport));
        }

        private async Task GenerateShiftReportAsync()
        {
            var shiftIdStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入班次 ID:", "生成班报", "1", -1, -1);
            if (!int.TryParse(shiftIdStr, out var shiftId)) return;

            var dateStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入日期 (yyyy-MM-dd):", "生成班报", DateTime.Now.ToString("yyyy-MM-dd"), -1, -1);
            if (!DateTime.TryParse(dateStr, out var date)) return;

            await GenerateReportAsync(ReportType.Shift, date, shiftId);
        }

        private async Task GenerateDailyReportAsync()
        {
            var dateStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入日期 (yyyy-MM-dd):", "生成日报", DateTime.Now.ToString("yyyy-MM-dd"), -1, -1);
            if (!DateTime.TryParse(dateStr, out var date)) return;

            await GenerateReportAsync(ReportType.Daily, date, 0);
        }

        private async Task GenerateMonthlyReportAsync()
        {
            var yearStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入年份:", "生成月报", DateTime.Now.Year.ToString(), -1, -1);
            if (!int.TryParse(yearStr, out var year)) return;

            var monthStr = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入月份 (1-12):", "生成月报", DateTime.Now.Month.ToString(), -1, -1);
            if (!int.TryParse(monthStr, out var month) || month < 1 || month > 12) return;

            await GenerateReportAsync(ReportType.Monthly, new DateTime(year, month, 1), 0);
        }

        private async Task GenerateReportAsync(ReportType type, DateTime date, int shiftId = 0)
        {
            try
            {
                IsGenerating = true;
                StatusMessage = $"正在生成{type}...";

                var report = shiftId > 0
                    ? await _reportService.GenerateShiftReportAsync(shiftId, date, "CurrentUser")
                    : type == ReportType.Daily
                        ? await _reportService.GenerateDailyReportAsync(date, "CurrentUser")
                        : await _reportService.GenerateMonthlyReportAsync(date.Year, date.Month, "CurrentUser");

                StatusMessage = $"报表生成成功: {report.ReportNumber}";
                await LoadReportsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "生成报表失败");
                StatusMessage = $"生成失败: {ex.Message}";
                MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void ViewReport()
        {
            if (SelectedReport == null) return;
            // TODO: 打开报表详情窗口
            MessageBox.Show($"查看报表: {SelectedReport.ReportNumber}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ApproveReportAsync()
        {
            if (SelectedReport == null) return;

            try
            {
                var result = await _reportService.ApproveReportAsync(SelectedReport.Id, "CurrentUser");
                if (result)
                {
                    StatusMessage = $"报表已审核: {SelectedReport.ReportNumber}";
                    await LoadReportsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "审核报表失败");
                MessageBox.Show($"审核失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportReportAsync()
        {
            if (SelectedReport == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF 文件 (*.pdf)|*.pdf|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json",
                FileName = $"{SelectedReport.ReportNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // TODO: 实际导出逻辑
                    StatusMessage = $"报表已导出: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出报表失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintReport()
        {
            if (SelectedReport == null) return;
            // TODO: 打印逻辑
            MessageBox.Show("打印功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DeleteReportAsync()
        {
            if (SelectedReport == null) return;

            var result = MessageBox.Show(
                $"确定要删除报表 '{SelectedReport.ReportNumber}' 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _reportService.DeleteReportAsync(SelectedReport.Id);
                    await LoadReportsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "删除报表失败");
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}