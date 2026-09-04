using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Serilog;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// 罐体物料分配视图模型
    /// </summary>
    public class TankMaterialAssignmentViewModel : DisposableViewModel
    {
        private readonly MaterialService _materialService;
        private readonly ILogger _logger;

        #region 字段

        private ObservableCollection<TankMaterial> _tankMaterials = new();
        private ObservableCollection<TankMaterial> _currentAssignments = new();
        private ObservableCollection<TankMaterialHistory> _tankMaterialHistory = new();
        private ObservableCollection<AlertItem> _alerts = new();
        private ObservableCollection<Material> _weighableMaterials = new();

        private TankMaterial? _selectedTankMaterial;
        private string _searchText = "";
        private bool _isLoading;
        private string _statusMessage = "就绪";

        #endregion

        #region 属性

        public ObservableCollection<TankMaterial> TankMaterials => _tankMaterials;
        public ObservableCollection<TankMaterial> CurrentAssignments => _currentAssignments;
        public ObservableCollection<TankMaterialHistory> TankMaterialHistory => _tankMaterialHistory;
        public ObservableCollection<AlertItem> Alerts => _alerts;
        public ObservableCollection<Material> WeighableMaterials => _weighableMaterials;

        public TankMaterial? SelectedTankMaterial
        {
            get => _selectedTankMaterial;
            set
            {
                if (SetProperty(ref _selectedTankMaterial, value))
                {
                    EditPanelVisibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                    _ = LoadHistoryAsync(value?.TankId ?? 0);
                    UpdateSelectionText();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
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

        public Visibility EditPanelVisibility { get; private set; } = Visibility.Collapsed;

        #endregion

        #region 命令

        public ICommand LoadCommand { get; }
        public ICommand AssignMaterialCommand { get; }
        public ICommand SaveAssignmentCommand { get; }
        public ICommand ChangeMaterialCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand ExportReportCommand { get; }

        #endregion

        public TankMaterialAssignmentViewModel(MaterialService materialService)
        {
            _materialService = materialService;
            _logger = LogManager.CreateModuleLogger(nameof(TankMaterialAssignmentViewModel));

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AssignMaterialCommand = new AsyncRelayCommand(AssignMaterialAsync);
            SaveAssignmentCommand = new AsyncRelayCommand(SaveAssignmentAsync);
            ChangeMaterialCommand = new AsyncRelayCommand(ChangeMaterialAsync);
            ViewHistoryCommand = new AsyncRelayCommand(ViewHistoryAsync);
            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载数据...";

                // 加载可称量物料
                var materials = await _materialService.GetWeighableMaterialsAsync();
                _weighableMaterials.Clear();
                foreach (var m in materials) _weighableMaterials.Add(m);

                // 加载罐体分配
                var assignments = await _materialService.GetAllTankMaterialsAsync();
                _tankMaterials.Clear();
                foreach (var a in assignments) _tankMaterials.Add(a);

                // 加载当前分配（仅主物料）
                _currentAssignments.Clear();
                foreach (var a in assignments.Where(x => x.IsPrimary && x.IsActive).OrderBy(x => x.TankCode))
                    _currentAssignments.Add(a);

                // 生成告警
                GenerateAlerts();

                StatusMessage = $"就绪 - {_tankMaterials.Count} 个分配记录, {_currentAssignments.Count} 个主物料罐体";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载罐体分配数据失败");
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadHistoryAsync(int tankId)
        {
            if (tankId == 0)
            {
                _tankMaterialHistory.Clear();
                return;
            }

            try
            {
                var history = await _materialService.GetTankMaterialHistoryAsync(tankId);
                _tankMaterialHistory.Clear();
                foreach (var h in history) _tankMaterialHistory.Add(h);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载罐体历史失败");
            }
        }

        private void GenerateAlerts()
        {
            _alerts.Clear();

            // 检查：同一罐体分配了多个主物料
            var duplicatePrimary = _tankMaterials
                .Where(x => x.IsPrimary && x.IsActive)
                .GroupBy(x => x.TankId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);

            foreach (var item in duplicatePrimary)
            {
                _alerts.Add(new AlertItem
                {
                    Time = DateTime.Now,
                    TankName = item.TankName ?? item.TankCode ?? "未知",
                    AlertType = "配置冲突",
                    Message = $"罐体同时分配了多个主物料: {item.MaterialName}",
                    Level = "Error"
                });
            }

            // 检查：罐体未分配物料
            var tanksWithoutMaterial = _tankMaterials
                .GroupBy(x => x.TankId)
                .Where(g => !g.Any(x => x.IsActive))
                .Select(g => g.First());

            foreach (var item in tanksWithoutMaterial)
            {
                _alerts.Add(new AlertItem
                {
                    Time = DateTime.Now,
                    TankName = item.TankName ?? item.TankCode ?? "未知",
                    AlertType = "未分配",
                    Message = "该罐体未分配任何物料",
                    Level = "Warning"
                });
            }

            // 检查：物料密度异常
            foreach (var item in _tankMaterials.Where(x => x.Material != null && x.IsActive))
            {
                if (item.Material.Density <= 0)
                {
                    _alerts.Add(new AlertItem
                    {
                        Time = DateTime.Now,
                        TankName = item.TankName ?? item.TankCode ?? "未知",
                        AlertType = "数据异常",
                        Message = $"物料 {item.MaterialName} 密度为 0 或负值，液位计算将不准确",
                        Level = "Warning"
                    });
                }
            }

            if (_alerts.Count == 0)
            {
                _alerts.Add(new AlertItem
                {
                    Time = DateTime.Now,
                    TankName = "系统",
                    AlertType = "正常",
                    Message = "所有罐体分配配置正常",
                    Level = "Success"
                });
            }
        }

        private void UpdateSelectionText()
        {
            if (SelectedTankMaterial != null)
            {
                StatusMessage = $"已选: {SelectedTankMaterial.TankName} ({SelectedTankMaterial.TankCode}) - {SelectedTankMaterial.MaterialName}";
            }
        }

        private async Task AssignMaterialAsync()
        {
            // TODO: 打开分配物料对话框
            MessageBox.Show("分配物料功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task SaveAssignmentAsync()
        {
            if (SelectedTankMaterial == null) return;

            try
            {
                await _materialService.UpdateTankMaterialAsync(SelectedTankMaterial, "CurrentUser");
                
                // 如果设置为主物料，取消其他主物料
                if (SelectedTankMaterial.IsPrimary)
                {
                    await _materialService.SetPrimaryAsync(SelectedTankMaterial.TankId, SelectedTankMaterial.MaterialId);
                }

                // 记录历史
                var history = new TankMaterialHistory
                {
                    TankId = SelectedTankMaterial.TankId,
                    TankCode = SelectedTankMaterial.TankCode,
                    TankName = SelectedTankMaterial.TankName,
                    NewMaterialId = SelectedTankMaterial.MaterialId,
                    ChangeReason = "更新分配配置",
                    Operator = "CurrentUser",
                    ChangeTime = DateTime.Now
                };
                await _materialService.AddTankMaterialHistoryAsync(history);

                MessageBox.Show("保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 刷新
                await LoadAsync();
                EditPanelVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存分配失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChangeMaterialAsync()
        {
            if (SelectedTankMaterial == null) return;

            // TODO: 打开物料选择对话框
            MessageBox.Show("变更物料功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ViewHistoryAsync()
        {
            if (SelectedTankMaterial == null)
            {
                MessageBox.Show("请先选择一个罐体", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // TODO: 切换到历史标签页或弹出历史窗口
            MessageBox.Show($"查看 {SelectedTankMaterial.TankName} 的变更历史", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportReportAsync()
        {
            // TODO: 导出Excel报表
            MessageBox.Show("导出报表功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 告警项
    /// </summary>
    public class AlertItem : INotifyPropertyChanged
    {
        private DateTime _time;
        private string _tankName = "";
        private string _alertType = "";
        private string _message = "";
        private string _level = "";

        public DateTime Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public string TankName
        {
            get => _tankName;
            set { _tankName = value; OnPropertyChanged(nameof(TankName)); }
        }

        public string AlertType
        {
            get => _alertType;
            set { _alertType = value; OnPropertyChanged(nameof(AlertType)); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        public string Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(nameof(Level)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}