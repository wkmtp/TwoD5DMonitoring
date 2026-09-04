using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Serilog;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : DisposableViewModel
    {
        private readonly ILogger _logger;
        private readonly PlcManager _plcManager;
        private readonly MaterialService _materialService;
        private readonly RecipeService _recipeService;
        private readonly PlcRecipeDownloadService _plcDownloadService;
        private readonly AlarmService _alarmService;

        #region 字段

        private int _currentTabIndex = 0;
        private string _statusMessage = "系统就绪";
        private string _currentMode = "设计器";
        private string _plcConnectionStatus = "PLC: 未连接";
        private string _databaseStatus = "数据库: 未连接";
        private string _alarmStatus = "报警: 0";

        private bool _isDesignerVisible = true;
        private bool _isRuntimeVisible = false;
        private bool _isMaterialVisible = false;
        private bool _isRecipeVisible = false;
        private bool _isTankVisible = false;
        private bool _is3DVisible = false;
        private bool _isAlarmVisible = false;
        private bool _isProductionReportVisible = false;
        private bool _isOeeVisible = false;
        private bool _isEnergyVisible = false;
        private bool _isTrendVisible = false;

        private DispatcherTimer _statusTimer;
        private ProjectDocument? _currentProject;

        #endregion

        #region 属性

        public int CurrentTabIndex
        {
            get => _currentTabIndex;
            set => SetProperty(ref _currentTabIndex, value, onChanged: OnTabChanged);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public string PlcConnectionStatus
        {
            get => _plcConnectionStatus;
            set => SetProperty(ref _plcConnectionStatus, value);
        }

        public string DatabaseStatus
        {
            get => _databaseStatus;
            set => SetProperty(ref _databaseStatus, value);
        }

        public string AlarmStatus
        {
            get => _alarmStatus;
            set => SetProperty(ref _alarmStatus, value);
        }

        public bool IsDesignerVisible
        {
            get => _isDesignerVisible;
            set => SetProperty(ref _isDesignerVisible, value);
        }

        public bool IsRuntimeVisible
        {
            get => _isRuntimeVisible;
            set => SetProperty(ref _isRuntimeVisible, value);
        }

        public bool IsMaterialVisible
        {
            get => _isMaterialVisible;
            set => SetProperty(ref _isMaterialVisible, value);
        }

        public bool IsRecipeVisible
        {
            get => _isRecipeVisible;
            set => SetProperty(ref _isRecipeVisible, value);
        }

        public bool IsTankVisible
        {
            get => _isTankVisible;
            set => SetProperty(ref _isTankVisible, value);
        }

        public bool Is3DVisible
        {
            get => _is3DVisible;
            set => SetProperty(ref _is3DVisible, value);
        }

        public bool IsAlarmVisible
        {
            get => _isAlarmVisible;
            set => SetProperty(ref _isAlarmVisible, value);
        }

        public bool IsProductionReportVisible
        {
            get => _isProductionReportVisible;
            set => SetProperty(ref _isProductionReportVisible, value);
        }

        public bool IsOeeVisible
        {
            get => _isOeeVisible;
            set => SetProperty(ref _isOeeVisible, value);
        }

        public bool IsEnergyVisible
        {
            get => _isEnergyVisible;
            set => SetProperty(ref _isEnergyVisible, value);
        }

        public bool IsTrendVisible
        {
            get => _isTrendVisible;
            set => SetProperty(ref _isTrendVisible, value);
        }

        public ProjectDocument? CurrentProject
        {
            get => _currentProject;
            set => SetProperty(ref _currentProject, value);
        }

        #endregion

        #region 命令

        public ICommand SwitchToDesignerCommand { get; }
        public ICommand SwitchToRuntimeCommand { get; }
        public ICommand SwitchToMaterialCommand { get; }
        public ICommand SwitchToRecipeCommand { get; }
        public ICommand SwitchToTankCommand { get; }
        public ICommand SwitchTo3DCommand { get; }
        public ICommand SwitchTo3DEditorCommand { get; }
        public ICommand SwitchToAlarmCommand { get; }
        public ICommand SwitchToProductionReportCommand { get; }
        public ICommand SwitchToOeeCommand { get; }
        public ICommand SwitchToEnergyCommand { get; }
        public ICommand SwitchToTrendCommand { get; }

        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand ImportConfigCommand { get; }
        public ICommand ExportConfigCommand { get; }
        public ICommand ExitCommand { get; }

        public ICommand OpenTagBindingCommand { get; }
        public ICommand OpenPerformanceCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenManualCommand { get; }
        public ICommand OpenAboutCommand { get; }

        #endregion

        public MainWindowViewModel(
            PlcManager plcManager,
            MaterialService materialService,
            RecipeService recipeService,
            PlcRecipeDownloadService plcDownloadService,
            AlarmService alarmService)
        {
            _logger = LogManager.CreateModuleLogger(nameof(MainWindowViewModel));
            _plcManager = plcManager;
            _materialService = materialService;
            _recipeService = recipeService;
            _plcDownloadService = plcDownloadService;
            _alarmService = alarmService;

            // 订阅报警事件
            _alarmService.AlarmRaised += OnAlarmRaised;
            _alarmService.AlarmAcknowledged += OnAlarmAcknowledged;
            _alarmService.AlarmReturned += OnAlarmReturned;

            // 初始化命令
            SwitchToDesignerCommand = new RelayCommand(() => SwitchTab(0));
            SwitchToRuntimeCommand = new RelayCommand(() => SwitchTab(1));
            SwitchToMaterialCommand = new RelayCommand(() => SwitchTab(2));
            SwitchToRecipeCommand = new RelayCommand(() => SwitchTab(3));
            SwitchToTankCommand = new RelayCommand(() => SwitchTab(4));
            SwitchTo3DCommand = new RelayCommand(() => SwitchTab(5));
            SwitchTo3DEditorCommand = new RelayCommand(Open3DEditor);
            SwitchToAlarmCommand = new RelayCommand(() => SwitchTab(6));
            SwitchToProductionReportCommand = new RelayCommand(() => SwitchTab(7));
            SwitchToOeeCommand = new RelayCommand(() => SwitchTab(8));
            SwitchToEnergyCommand = new RelayCommand(() => SwitchTab(9));
            SwitchToTrendCommand = new RelayCommand(() => SwitchTab(10));

            NewProjectCommand = new AsyncRelayCommand(NewProjectAsync);
            OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
            ImportConfigCommand = new AsyncRelayCommand(ImportConfigAsync);
            ExportConfigCommand = new AsyncRelayCommand(ExportConfigAsync);
            ExitCommand = new RelayCommand(ExitApplication);

            OpenTagBindingCommand = new RelayCommand(OpenTagBinding);
            OpenPerformanceCommand = new RelayCommand(OpenPerformance);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenManualCommand = new RelayCommand(OpenManual);
            OpenAboutCommand = new RelayCommand(OpenAbout);

            // 状态更新定时器
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _statusTimer.Tick += OnStatusTimerTick;

            // 初始化新项目
            _currentProject = new ProjectDocument();

            _logger.Information("MainWindowViewModel 初始化完成");
        }

        private void OnAlarmRaised(object? sender, AlarmEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"报警触发: {e.Definition.AlarmName} = {e.AlarmInstance.CurrentValue}";
            });
        }

        private void OnAlarmAcknowledged(object? sender, AlarmEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"报警确认: {e.Definition.AlarmName} 由 {e.AlarmInstance.AckedBy} 确认";
            });
        }

        private void OnAlarmReturned(object? sender, AlarmEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"报警恢复: {e.Definition.AlarmName}";
            });
        }

        private void OnTabChanged()
        {
            UpdateTabVisibility();
            UpdateCurrentMode();
        }

        private void UpdateTabVisibility()
        {
            IsDesignerVisible = CurrentTabIndex == 0;
            IsRuntimeVisible = CurrentTabIndex == 1;
            IsMaterialVisible = CurrentTabIndex == 2;
            IsRecipeVisible = CurrentTabIndex == 3;
            IsTankVisible = CurrentTabIndex == 4;
            Is3DVisible = CurrentTabIndex == 5;
            IsAlarmVisible = CurrentTabIndex == 6;
            IsProductionReportVisible = CurrentTabIndex == 7;
            IsOeeVisible = CurrentTabIndex == 8;
            IsEnergyVisible = CurrentTabIndex == 9;
            IsTrendVisible = CurrentTabIndex == 10;
        }

        private void UpdateCurrentMode()
        {
            CurrentMode = CurrentTabIndex switch
            {
                0 => "设计器",
                1 => "运行时",
                2 => "物料管理",
                3 => "配方管理",
                4 => "罐体分配",
                5 => "3D监控",
                6 => "报警管理",
                7 => "生产报表",
                8 => "OEE分析",
                9 => "能耗统计",
                10 => "历史趋势",
                _ => "未知"
            };
        }

        private void SwitchTab(int index)
        {
            CurrentTabIndex = index;
        }

        private async void OnStatusTimerTick(object? sender, EventArgs e)
        {
            try
            {
                // 更新 PLC 连接状态
                var states = _plcManager.GetAllConnectionStates();
                if (states.Count > 0)
                {
                    var connectedCount = states.Count(s => s.Value == PlcConnectionState.Connected);
                    var totalCount = states.Count;
                    PlcConnectionStatus = $"PLC: {connectedCount}/{totalCount} 连接";
                    
                    if (connectedCount == 0)
                        PlcConnectionStatus += " (全部断开)";
                    else if (connectedCount < totalCount)
                        PlcConnectionStatus += " (部分断开)";
                }
                else
                {
                    PlcConnectionStatus = "PLC: 未配置";
                }

                // 更新报警状态
                var activeAlarms = _alarmService.GetActiveAlarmsCount();
                AlarmStatus = $"报警: {activeAlarms}";

                // 更新数据库状态
                try
                {
                    var dbProvider = TwoD5DMonitoring.Infrastructure.Database.DatabaseProviderFactory.GetProvider();
                    var connected = dbProvider.TestConnection(TwoD5DMonitoring.Configuration.ConfigManager.Current.Database.ConnectionString);
                    DatabaseStatus = connected ? "数据库: 已连接" : "数据库: 连接失败";
                }
                catch
                {
                    DatabaseStatus = "数据库: 未配置";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "状态更新失败");
            }
        }

        public void StartStatusTimer()
        {
            _statusTimer.Start();
        }

        public void StopStatusTimer()
        {
            _statusTimer.Stop();
        }

        #region 文件操作

        private async Task NewProjectAsync()
        {
            var result = MessageBox.Show(
                "确定要创建新项目吗？未保存的更改将丢失。",
                "新建项目", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentProject = new ProjectDocument();
                StatusMessage = "已创建新项目";
                _logger.Information("创建新项目");
            }
        }

        private async Task OpenProjectAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "组态文件 (*.config)|*.config|所有文件 (*.*)|*.*",
                Title = "打开项目"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(dialog.FileName);
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigModel>(json);
                    
                    if (config != null)
                    {
                        // 转换为 ProjectDocument
                        _currentProject = ConvertConfigToProject(config);
                        StatusMessage = $"已加载项目: {System.IO.Path.GetFileName(dialog.FileName)}";
                        _logger.Information("打开项目: {File}", dialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "打开项目失败");
                    MessageBox.Show($"打开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveProjectAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "组态文件 (*.config)|*.config|所有文件 (*.*)|*.*",
                Title = "保存项目",
                FileName = $"project_{DateTime.Now:yyyyMMdd_HHmmss}.config"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 从设计器获取当前项目
                    // 这里需要根据实际情况实现
                    var config = ConvertProjectToConfig(_currentProject!);
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(dialog.FileName, json);

                    StatusMessage = $"项目已保存: {System.IO.Path.GetFileName(dialog.FileName)}";
                    _logger.Information("保存项目: {File}", dialog.FileName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "保存项目失败");
                    MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ImportConfigAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "配置文件 (*.config)|*.config|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "导入配置"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(dialog.FileName);
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigModel>(json);
                    
                    if (config != null)
                    {
                        // 加载到设计器
                        StatusMessage = $"已导入配置: {System.IO.Path.GetFileName(dialog.FileName)}";
                        _logger.Information("导入配置: {File}", dialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导入配置失败");
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportConfigAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "配置文件 (*.config)|*.config|JSON文件 (*.json)|*.json",
                Title = "导出配置",
                FileName = $"config_{DateTime.Now:yyyyMMdd_HHmmss}.config"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var config = ConvertProjectToConfig(_currentProject!);
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(dialog.FileName, json);

                    StatusMessage = $"配置已导出: {System.IO.Path.GetFileName(dialog.FileName)}";
                    _logger.Information("导出配置: {File}", dialog.FileName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "导出配置失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region 工具命令

        private void OpenTagBinding()
        {
            StatusMessage = "打开标签绑定对话框";
            // TODO: 实现标签绑定对话框
        }

        private void OpenPerformance()
        {
            StatusMessage = "打开性能监控";
            // TODO: 实现性能监控窗口
        }

        private void OpenSettings()
        {
            StatusMessage = "打开系统设置";
            // TODO: 实现系统设置窗口
        }

        private void OpenManual()
        {
            StatusMessage = "打开使用手册";
            // TODO: 打开帮助文档
        }

        private void OpenAbout()
        {
            MessageBox.Show(
                "TwoD5DMonitoring 2.5D 矢量组态监控系统\n" +
                "版本: 1.0.0\n" +
                "基于 .NET 8.0 WPF 开发\n" +
                "支持: MySQL, PostgreSQL, SQL Server\n" +
                "PLC: OPC UA (西门子/欧姆龙/三菱)",
                "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Open3DEditor()
        {
            // TODO: 打开 3D 编辑器窗口
            StatusMessage = "打开 3D 编辑器";
        }

        #endregion

        #region 转换辅助

        private ProjectDocument ConvertConfigToProject(ConfigModel config)
        {
            var project = new ProjectDocument
            {
                Version = config.Version,
                CanvasWidth = config.CanvasWidth,
                CanvasHeight = config.CanvasHeight,
                GridSize = config.GridSize,
                SnapToGrid = config.SnapToGrid
            };

            // 转换设备
            foreach (var equipConfig in config.Equipments)
            {
                var equip = CreateEquipmentFromConfig(equipConfig);
                if (equip != null)
                    project.Equipments.Add(equip);
            }

            // 转换管道
            foreach (var pipeConfig in config.Pipes)
            {
                var pipe = CreatePipeFromConfig(pipeConfig);
                if (pipe != null)
                    project.Pipes.Add(pipe);
            }

            // 转换连接
            foreach (var connConfig in config.Connections)
            {
                project.Connections.Add(new ConnectionInfo
                {
                    Id = connConfig.Id,
                    FromElementId = connConfig.FromEquipmentId,
                    FromAnchorId = connConfig.FromAnchor.ToString(),
                    ToElementId = connConfig.ToEquipmentId,
                    ToAnchorId = connConfig.ToAnchor.ToString(),
                    PipeId = connConfig.PipeId
                });
            }

            return project;
        }

        private ConfigModel ConvertProjectToConfig(ProjectDocument project)
        {
            var config = new ConfigModel
            {
                Version = project.Version,
                CanvasWidth = project.CanvasWidth,
                CanvasHeight = project.CanvasHeight,
                GridSize = project.GridSize,
                SnapToGrid = project.SnapToGrid
            };

            // 转换设备
            foreach (var equip in project.Equipments)
            {
                config.Equipments.Add(CreateConfigFromEquipment(equip));
            }

            // 转换管道
            foreach (var pipe in project.Pipes)
            {
                config.Pipes.Add(CreateConfigFromPipe(pipe));
            }

            // 转换连接
            foreach (var conn in project.Connections)
            {
                config.Connections.Add(new ConnectionConfig
                {
                    Id = conn.Id,
                    FromEquipmentId = conn.FromElementId,
                    FromAnchor = Enum.Parse<Models.AnchorType>(conn.FromAnchorId),
                    ToEquipmentId = conn.ToElementId,
                    ToAnchor = Enum.Parse<Models.AnchorType>(conn.ToAnchorId),
                    PipeId = conn.PipeId
                });
            }

            return config;
        }

        private EquipmentBase? CreateEquipmentFromConfig(EquipmentConfig config)
        {
            return config.Type switch
            {
                EquipmentType.RootBlower => new RootBlowerModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.DustCollector => new DustCollectorModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.FeedBin => new FeedBinModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.PressurizedTank => new PressurizedTankModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.CarbonBlackTank => new CarbonBlackTankModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.OilTank => new OilTankModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.Mixer => new MixerModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.CarbonBlackScale => new CarbonBlackScaleModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.OilScale => new OilScaleModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.RubberScale => new RubberScaleModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.RubberConveyor => new RubberConveyorModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.OilPump => new OilPumpModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.SideBlowValve => new SideBlowValveModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.ThreeWayValve => new ThreeWayValveModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.TwoWayValve => new TwoWayValveModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.PipeReducer => new PipeReducerModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                EquipmentType.SideBlowDevice => new SideBlowDeviceModel { Id = config.Id, Name = config.Name, Position = new System.Windows.Point(config.X, config.Y), Width = config.Width, Height = config.Height, Rotation = config.Rotation },
                _ => null
            };
        }

        private EquipmentConfig CreateConfigFromEquipment(EquipmentBase equip)
        {
            var config = equip.Type switch
            {
                EquipmentType.RootBlower => (EquipmentConfig)new RootBlowerConfig(),
                EquipmentType.DustCollector => (EquipmentConfig)new DustCollectorConfig(),
                EquipmentType.FeedBin => (EquipmentConfig)new FeedBinConfig(),
                EquipmentType.PressurizedTank => (EquipmentConfig)new PressurizedTankConfig(),
                EquipmentType.CarbonBlackTank => (EquipmentConfig)new CarbonBlackTankConfig(),
                EquipmentType.OilTank => (EquipmentConfig)new OilTankConfig(),
                EquipmentType.Mixer => (EquipmentConfig)new MixerConfig(),
                EquipmentType.CarbonBlackScale => (EquipmentConfig)new CarbonBlackScaleConfig(),
                EquipmentType.OilScale => (EquipmentConfig)new OilScaleConfig(),
                EquipmentType.RubberScale => (EquipmentConfig)new RubberScaleConfig(),
                EquipmentType.RubberConveyor => (EquipmentConfig)new RubberConveyorConfig(),
                EquipmentType.OilPump => (EquipmentConfig)new OilPumpConfig(),
                EquipmentType.SideBlowValve => (EquipmentConfig)new SideBlowValveConfig(),
                EquipmentType.ThreeWayValve => (EquipmentConfig)new ThreeWayValveConfig(),
                EquipmentType.TwoWayValve => (EquipmentConfig)new TwoWayValveConfig(),
                EquipmentType.PipeReducer => (EquipmentConfig)new PipeReducerConfig(),
                EquipmentType.SideBlowDevice => (EquipmentConfig)new SideBlowDeviceConfig(),
                _ => throw new InvalidOperationException($"未知设备类型: {equip.Type}")
            };

            config.Id = equip.Id;
            config.Name = equip.Name;
            config.X = equip.Position.X;
            config.Y = equip.Position.Y;
            config.Width = equip.Width;
            config.Height = equip.Height;
            config.Rotation = equip.Rotation;
            config.TagBindings = equip.TagBindings;

            return config;
        }

        private PipeSegment? CreatePipeFromConfig(PipeConfig config)
        {
            var pipe = config.Type switch
            {
                PipeType.CarbonConveying => new StraightPipe(),
                PipeType.OilSupply => new StraightPipe(),
                PipeType.CoolingWater => new StraightPipe(),
                PipeType.CompressedAir => new StraightPipe(),
                PipeType.DustCollection => new StraightPipe(),
                _ => new StraightPipe()
            };

            pipe.Id = config.Id;
            pipe.Name = config.Name ?? "";
            pipe.PipeType = config.Type;
            pipe.StartPoint = new System.Windows.Point(config.StartPoint.X, config.StartPoint.Y);
            pipe.EndPoint = new System.Windows.Point(config.EndPoint.X, config.EndPoint.Y);
            pipe.Diameter = config.Diameter;
            pipe.FlowSpeed = config.FlowSpeed;
            pipe.FlowDirection = config.FlowDirection;
            pipe.IsAnimating = config.HasAnimation;
            pipe.StartAnchorId = config.ValveIds.FirstOrDefault() ?? "";
            pipe.EndAnchorId = config.ValveIds.LastOrDefault() ?? "";

            foreach (var cp in config.ControlPoints)
            {
                pipe.ControlPoints.Add(new System.Windows.Point(cp.X, cp.Y));
            }

            return pipe;
        }

        private PipeConfig CreateConfigFromPipe(PipeSegment pipe)
        {
            return new PipeConfig
            {
                Id = pipe.Id,
                Name = pipe.Name ?? "",
                Type = pipe.PipeType,
                StartPoint = new PointConfig { X = pipe.StartPoint.X, Y = pipe.StartPoint.Y },
                EndPoint = new PointConfig { X = pipe.EndPoint.X, Y = pipe.EndPoint.Y },
                ControlPoints = new ObservableCollection<PointConfig>(
                    pipe.ControlPoints.Select(cp => new PointConfig { X = cp.X, Y = cp.Y })),
                Diameter = pipe.Diameter,
                FlowSpeed = pipe.FlowSpeed,
                FlowDirection = pipe.FlowDirection,
                HasAnimation = pipe.IsAnimating
            };
        }

        #endregion

        protected override void OnDisposing()
        {
            _statusTimer?.Stop();
            base.OnDisposing();
        }
    }
}