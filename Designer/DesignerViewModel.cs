using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Controls;
using TwoD5DMonitoring.Controls.Equipment;
using TwoD5DMonitoring.Controls.Pipes;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Designer
{
    public class DesignerViewModel : INotifyPropertyChanged
    {
        #region 字段

        private Canvas _designCanvas;
        private Canvas _pipeCanvas;
        private ItemsControl _elementsControl;
        private TreeView _objectTree;
        private ListBox _outputList;
        private StackPanel _propertyPanelControl;
        private ProjectDocument _project;
        private ToolboxItem _selectedTool;
        private BaseEquipmentControl _selectedElement;
        private bool _showGrid = true;
        private bool _snapToGrid = true;
        private double _gridSize = 20;
        private bool _isDirty;
        private Stack<ICommand> _undoStack = new();
        private Stack<ICommand> _redoStack = new();

        #endregion

        #region 属性

        public ObservableCollection<ToolboxItem> ConveyingTools { get; } = new();
        public ObservableCollection<ToolboxItem> UpperTools { get; } = new();
        public ObservableCollection<ToolboxItem> PipeTools { get; } = new();
        public ObservableCollection<BaseEquipmentControl> Elements { get; } = new();
        public ObservableCollection<PipeSegment> Pipes { get; } = new();
        public ObservableCollection<TreeNode> ProjectElements { get; } = new();

        public ToolboxItem SelectedTool
        {
            get => _selectedTool;
            set { _selectedTool = value; OnPropertyChanged(nameof(SelectedTool)); }
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set { _showGrid = value; OnPropertyChanged(nameof(ShowGrid)); }
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set { _snapToGrid = value; OnPropertyChanged(nameof(SnapToGrid)); }
        }

        public double GridSize
        {
            get => _gridSize;
            set { _gridSize = value; OnPropertyChanged(nameof(GridSize)); }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(nameof(IsDirty)); }
        }

        public ProjectDocument Project => _project;

        public BaseEquipmentControl SelectedElement
        {
            get => _selectedElement;
            set 
            { 
                _selectedElement = value; 
                OnPropertyChanged(nameof(SelectedElement));
                UpdatePropertyPanel();
            }
        }

        #endregion

        #region 命令

        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand ToggleGridCommand { get; }
        public ICommand ToggleSnapCommand { get; }
        public ICommand SwitchToRuntimeCommand { get; }
        public ICommand DeleteElementCommand { get; }

        #endregion

public DesignerViewModel()
        {
            InitializeTools();
            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            SaveProjectCommand = new RelayCommand(SaveProject, () => IsDirty);
            UndoCommand = new RelayCommand(Undo, () => CanUndo);
            RedoCommand = new RelayCommand(Redo, () => CanRedo);
            ToggleGridCommand = new RelayCommand(() => ShowGrid = !ShowGrid);
            ToggleSnapCommand = new RelayCommand(() => SnapToGrid = !SnapToGrid);
            SwitchToRuntimeCommand = new RelayCommand(SwitchToRuntime);
            DeleteElementCommand = new RelayCommand(DeleteSelectedElement, () => SelectedElement != null);
        }

        public void Initialize(Canvas designCanvas, ItemsControl pipeControl, ItemsControl elementsControl, TreeView objectTree, ListBox outputList, StackPanel propertyPanelControl)
        {
            _designCanvas = designCanvas;
            _elementsControl = elementsControl;
            _objectTree = objectTree;
            _outputList = outputList;
            _propertyPanelControl = propertyPanelControl;

            // 从 pipeControl 的 ItemsPanel 获取 Canvas - 使用 Loaded 事件
            if (pipeControl != null)
            {
                pipeControl.Loaded += (s, e) =>
                {
                    // 尝试从视觉树中查找 Canvas
                    _pipeCanvas = FindVisualChild<Canvas>(pipeControl);
                };
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private void InitializeTools()
        {
            // 输送部分
            ConveyingTools.Add(new ToolboxItem { Name = "罗茨风机", Category = "输送", Type = EquipmentType.RootBlower, Description = "罗茨风机", Icon = CreateIcon("⛲") });
            ConveyingTools.Add(new ToolboxItem { Name = "除尘器", Category = "输送", Type = EquipmentType.DustCollector, Description = "除尘器", Icon = CreateIcon("🌀") });
            ConveyingTools.Add(new ToolboxItem { Name = "投料仓", Category = "输送", Type = EquipmentType.FeedBin, Description = "投料仓", Icon = CreateIcon("📦") });
            ConveyingTools.Add(new ToolboxItem { Name = "压送仓", Category = "输送", Type = EquipmentType.PressurizedTank, Description = "压送仓", Icon = CreateIcon("🗃️") });
            ConveyingTools.Add(new ToolboxItem { Name = "侧吹阀", Category = "输送", Type = EquipmentType.SideBlowValve, Description = "侧吹阀", Icon = CreateIcon("💨") });
            ConveyingTools.Add(new ToolboxItem { Name = "三通阀", Category = "输送", Type = EquipmentType.ThreeWayValve, Description = "三通分流阀", Icon = CreateIcon("⟳") });
            ConveyingTools.Add(new ToolboxItem { Name = "两通阀", Category = "输送", Type = EquipmentType.TwoWayValve, Description = "两通切换阀", Icon = CreateIcon("⟲") });

            // 上辅机设备
            UpperTools.Add(new ToolboxItem { Name = "炭黑罐", Category = "上辅机", Type = EquipmentType.CarbonBlackTank, Description = "炭黑储罐", Icon = CreateIcon("🛢️") });
            UpperTools.Add(new ToolboxItem { Name = "油罐", Category = "上辅机", Type = EquipmentType.OilTank, Description = "油料储罐", Icon = CreateIcon("🛢️") });
            UpperTools.Add(new ToolboxItem { Name = "炭黑秤", Category = "上辅机", Type = EquipmentType.CarbonBlackScale, Description = "炭黑称量秤", Icon = CreateIcon("⚖️") });
            UpperTools.Add(new ToolboxItem { Name = "油秤", Category = "上辅机", Type = EquipmentType.OilScale, Description = "油料称量秤", Icon = CreateIcon("⚖️") });
            UpperTools.Add(new ToolboxItem { Name = "密炼机", Category = "上辅机", Type = EquipmentType.Mixer, Description = "密炼机", Icon = CreateIcon("🏭") });
            UpperTools.Add(new ToolboxItem { Name = "胶料秤", Category = "上辅机", Type = EquipmentType.RubberScale, Description = "胶料称量秤", Icon = CreateIcon("⚖️") });
            UpperTools.Add(new ToolboxItem { Name = "胶料输送带", Category = "上辅机", Type = EquipmentType.RubberConveyor, Description = "胶料输送带", Icon = CreateIcon("🏭") });
            UpperTools.Add(new ToolboxItem { Name = "油泵", Category = "上辅机", Type = EquipmentType.OilPump, Description = "油泵", Icon = CreateIcon("⛽") });

            // 管路阀门
            PipeTools.Add(new ToolboxItem { Name = "直管", Category = "管路", Type = EquipmentType.PipeStraight, Description = "直管段", Icon = CreateIcon("➖") });
            PipeTools.Add(new ToolboxItem { Name = "90°弯管", Category = "管路", Type = EquipmentType.PipeElbow90, Description = "90度弯管", Icon = CreateIcon("↪") });
            PipeTools.Add(new ToolboxItem { Name = "三通管", Category = "管路", Type = EquipmentType.PipeTee, Description = "三通管", Icon = CreateIcon("⟳") });
            PipeTools.Add(new ToolboxItem { Name = "变径管", Category = "管路", Type = EquipmentType.PipeReducer, Description = "变径管", Icon = CreateIcon("🔄") });
            PipeTools.Add(new ToolboxItem { Name = "侧吹装置", Category = "管路", Type = EquipmentType.SideBlowDevice, Description = "侧吹装置", Icon = CreateIcon("💨") });
            PipeTools.Add(new ToolboxItem { Name = "两通阀", Category = "阀门", Type = EquipmentType.TwoWayValve, Description = "两通切换阀", Icon = CreateIcon("⟲") });
            PipeTools.Add(new ToolboxItem { Name = "三通阀", Category = "阀门", Type = EquipmentType.ThreeWayValve, Description = "三通分流阀", Icon = CreateIcon("⟳") });
        }

        private object CreateIcon(string emoji)
        {
            var textBlock = new TextBlock
            {
                Text = emoji,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return textBlock;
        }

        #region 项目管理

        public void NewProject()
        {
            if (IsDirty)
            {
                var result = MessageBox.Show("当前项目未保存，是否保存？", "提示", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes) SaveProject();
            }

            _project = new ProjectDocument();
            Elements.Clear();
            Pipes.Clear();
            ProjectElements.Clear();
            IsDirty = false;
            AddLog("新建项目");
        }

        public void OpenProject()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "组态文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    _project = JsonConvert.DeserializeObject<ProjectDocument>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    LoadProject(_project);
                    AddLog($"已打开: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveProject()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "组态文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 保存前同步数据
                    _project.Equipments.Clear();
                    foreach (var elem in Elements)
                    {
                        _project.Equipments.Add(elem.EquipmentModel);
                    }

                    _project.Pipes.Clear();
                    foreach (var pipe in Pipes)
                    {
                        _project.Pipes.Add(pipe);
                    }

                    var json = JsonConvert.SerializeObject(_project, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    File.WriteAllText(dialog.FileName, json);
                    _isDirty = false;
                    AddLog($"已保存: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadProject(ProjectDocument project)
        {
            Elements.Clear();
            Pipes.Clear();
            ProjectElements.Clear();

            // 加载设备
            foreach (var equip in project.Equipments)
            {
                var ctrl = CreateEquipmentControl(equip);
                if (ctrl != null)
                {
                    Elements.Add(ctrl);
                }
            }

            // 加载管道
            foreach (var pipe in project.Pipes)
            {
                Pipes.Add(pipe);
            }

            RebuildObjectTree();
            IsDirty = false;
        }

        #endregion

        #region ConfigModel 导入导出

        public void SaveAsConfig()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "组态配置文件 (*.config)|*.config|所有文件 (*.*)|*.*",
                DefaultExt = ".config"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var config = ConvertToConfigModel();
                    var json = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    File.WriteAllText(dialog.FileName, json);
                    AddLog($"已导出配置: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LoadFromConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "组态配置文件 (*.config)|*.config|所有文件 (*.*)|*.*",
                DefaultExt = ".config"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var config = JsonConvert.DeserializeObject<ConfigModel>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    LoadFromConfigModel(config);
                    AddLog($"已导入配置: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private ConfigModel ConvertToConfigModel()
        {
            var config = new ConfigModel
            {
                Version = _project.Version,
                CanvasWidth = _project.CanvasWidth,
                CanvasHeight = _project.CanvasHeight,
                GridSize = _project.GridSize,
                SnapToGrid = _project.SnapToGrid
            };

            foreach (var elem in Elements)
            {
                var equipConfig = CreateEquipmentConfig(elem.EquipmentModel);
                config.Equipments.Add(equipConfig);
            }

            foreach (var pipe in _project.Pipes)
            {
                var pipeConfig = new PipeConfig
                {
                    Id = pipe.Id,
                    Type = pipe.PipeType,
                    StartPoint = new PointConfig { X = pipe.StartPoint.X, Y = pipe.StartPoint.Y },
                    EndPoint = new PointConfig { X = pipe.EndPoint.X, Y = pipe.EndPoint.Y },
                    Diameter = pipe.Diameter,
                    FlowDirection = pipe.FlowDirection,
                    FlowSpeed = pipe.FlowSpeed,
                    HasAnimation = pipe.IsAnimating,
                    Color = pipe.PipeType switch
                    {
                        PipeType.CarbonConveying => "#1A1A1A",
                        PipeType.OilSupply => "#FFD700",
                        PipeType.CoolingWater => "#0066CC",
                        PipeType.CompressedAir => "#88AAAA",
                        PipeType.DustCollection => "#333333",
                        _ => "#888888"
                    }
                };

                foreach (var cp in pipe.ControlPoints)
                {
                    pipeConfig.ControlPoints.Add(new PointConfig { X = cp.X, Y = cp.Y });
                }

                config.Pipes.Add(pipeConfig);
            }

            foreach (var conn in _project.Connections)
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

        private void LoadFromConfigModel(ConfigModel config)
        {
            _project = new ProjectDocument
            {
                Version = config.Version,
                CanvasWidth = config.CanvasWidth,
                CanvasHeight = config.CanvasHeight,
                GridSize = config.GridSize,
                SnapToGrid = config.SnapToGrid
            };

            Elements.Clear();
            Pipes.Clear();
            ProjectElements.Clear();

            // 加载设备
            foreach (var equipConfig in config.Equipments)
            {
                var model = CreateEquipmentModelFromConfig(equipConfig);
                if (model != null)
                {
                    var ctrl = CreateEquipmentControl(model);
                    if (ctrl != null)
                    {
                        Elements.Add(ctrl);
                        _project.Equipments.Add(model);
                    }
                }
            }

            // 加载管道
            foreach (var pipeConfig in config.Pipes)
            {
                // 根据管道类型创建具体的管道段实例
                PipeSegment pipe = pipeConfig.Type switch
                {
                    PipeType.CarbonConveying => new StraightPipe(),
                    PipeType.OilSupply => new StraightPipe(),
                    PipeType.CoolingWater => new StraightPipe(),
                    PipeType.CompressedAir => new StraightPipe(),
                    PipeType.DustCollection => new StraightPipe(),
                    _ => new StraightPipe()
                };

                pipe.Id = pipeConfig.Id;
                pipe.PipeType = pipeConfig.Type;
                pipe.StartPoint = new Point(pipeConfig.StartPoint.X, pipeConfig.StartPoint.Y);
                pipe.EndPoint = new Point(pipeConfig.EndPoint.X, pipeConfig.EndPoint.Y);
                pipe.Diameter = pipeConfig.Diameter;
                pipe.FlowDirection = pipeConfig.FlowDirection;
                pipe.FlowSpeed = pipeConfig.FlowSpeed;
                pipe.IsAnimating = pipeConfig.HasAnimation;

                foreach (var cp in pipeConfig.ControlPoints)
                {
                    pipe.ControlPoints.Add(new Point(cp.X, cp.Y));
                }

                Pipes.Add(pipe);
                _project.Pipes.Add(pipe);
            }

// 加载连接
            foreach (var connConfig in config.Connections)
            {
                _project.Connections.Add(new ConnectionInfo
                {
                    Id = connConfig.Id,
                    FromElementId = connConfig.FromEquipmentId,
                    FromAnchorId = connConfig.FromAnchor.ToString(),
                    ToElementId = connConfig.ToEquipmentId,
                    ToAnchorId = connConfig.ToAnchor.ToString(),
                    PipeId = connConfig.PipeId
                });
            }

            RebuildObjectTree();
            IsDirty = false;
        }

        private EquipmentConfig CreateEquipmentConfig(EquipmentBase model)
        {
            return model switch
            {
                RootBlowerModel m => new RootBlowerConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    ImpellerCount = m.ImpellerCount,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                DustCollectorModel m => new DustCollectorConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                FeedBinModel m => new FeedBinConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                PressurizedTankModel m => new PressurizedTankConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                CarbonBlackTankModel m => new CarbonBlackTankConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                OilTankModel m => new OilTankConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                CarbonBlackScaleModel m => new CarbonBlackScaleConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                OilScaleModel m => new OilScaleConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                MixerModel m => new MixerConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                RubberScaleModel m => new RubberScaleConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                RubberConveyorModel m => new RubberConveyorConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                OilPumpModel m => new OilPumpConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                PipeReducerModel m => new PipeReducerConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    ReducerType = m.ReducerType,
                    InletDiameter = m.InletDiameter,
                    OutletDiameter = m.OutletDiameter,
                    Length = m.Length,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                SideBlowDeviceModel m => new SideBlowDeviceConfig
                {
                    Id = model.Id,
                    Name = model.Name,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    Width = model.Width,
                    Height = model.Height,
                    Rotation = model.Rotation,
                    MainDiameter = m.MainDiameter,
                    BranchDiameter = m.BranchDiameter,
                    BranchAngle = m.BranchAngle,
                    ValveState = m.ValveState,
                    TagBindings = m.TagBindings ?? new Dictionary<string, string>()
                },
                _ => new RootBlowerConfig { Id = model.Id, Name = model.Name, X = model.Position.X, Y = model.Position.Y }
            };
        }

        private EquipmentBase CreateEquipmentModelFromConfig(EquipmentConfig config)
        {
            return config switch
            {
                RootBlowerConfig c => new RootBlowerModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    ImpellerCount = c.ImpellerCount,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                DustCollectorConfig c => new DustCollectorModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                FeedBinConfig c => new FeedBinModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                PressurizedTankConfig c => new PressurizedTankModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                CarbonBlackTankConfig c => new CarbonBlackTankModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                OilTankConfig c => new OilTankModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                CarbonBlackScaleConfig c => new CarbonBlackScaleModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                OilScaleConfig c => new OilScaleModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                MixerConfig c => new MixerModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                RubberScaleConfig c => new RubberScaleModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                RubberConveyorConfig c => new RubberConveyorModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                OilPumpConfig c => new OilPumpModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                PipeReducerConfig c => new PipeReducerModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    ReducerType = c.ReducerType,
                    InletDiameter = c.InletDiameter,
                    OutletDiameter = c.OutletDiameter,
                    Length = c.Length,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                SideBlowDeviceConfig c => new SideBlowDeviceModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Position = new Point(c.X, c.Y),
                    Width = c.Width,
                    Height = c.Height,
                    Rotation = c.Rotation,
                    MainDiameter = c.MainDiameter,
                    BranchDiameter = c.BranchDiameter,
                    BranchAngle = c.BranchAngle,
                    ValveState = c.ValveState,
                    TagBindings = c.TagBindings ?? new Dictionary<string, string>()
                },
                _ => null
            };
        }

        #endregion

        #region 元素创建

        public BaseEquipmentControl CreateElement(EquipmentType type, Point position)
        {
            var equip = CreateEquipmentModel(type);
            equip.Position = position;

            var ctrl = CreateEquipmentControl(equip);
            if (ctrl != null)
            {
                Elements.Add(ctrl);
                RebuildObjectTree();
                IsDirty = true;
                AddLog($"创建元素: {equip.Name} ({equip.Type})");
            }
            return ctrl;
        }

        public void DeselectAll()
        {
            foreach (var elem in Elements)
            {
                elem.IsSelected = false;
            }
            SelectedTool = null;
            SelectedElement = null;
        }

        private EquipmentBase CreateEquipmentModel(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.RootBlower => new RootBlowerModel { Name = $"罗茨风机-{Elements.Count + 1}", Position = new Point(100, 100), Width = 180, Height = 120 },
                EquipmentType.DustCollector => new DustCollectorModel { Name = $"除尘器-{Elements.Count + 1}", Position = new Point(100, 100), Width = 160, Height = 280 },
                EquipmentType.FeedBin => new FeedBinModel { Name = $"投料仓-{Elements.Count + 1}", Position = new Point(100, 100), Width = 140, Height = 220 },
                EquipmentType.PressurizedTank => new PressurizedTankModel { Name = $"压送仓-{Elements.Count + 1}", Position = new Point(100, 100), Width = 120, Height = 220 },
                EquipmentType.CarbonBlackTank => new CarbonBlackTankModel { Name = $"炭黑罐-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 250 },
                EquipmentType.OilTank => new OilTankModel { Name = $"油罐-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 250 },
                EquipmentType.CarbonBlackScale => new CarbonBlackScaleModel { Name = $"炭黑秤-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 140 },
                EquipmentType.OilScale => new OilScaleModel { Name = $"油秤-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 140 },
                EquipmentType.Mixer => new MixerModel { Name = $"密炼机-{Elements.Count + 1}", Position = new Point(100, 100), Width = 300, Height = 200 },
                EquipmentType.RubberScale => new RubberScaleModel { Name = $"胶料秤-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 140 },
                EquipmentType.RubberConveyor => new RubberConveyorModel { Name = $"胶料输送带-{Elements.Count + 1}", Position = new Point(100, 100), Width = 300, Height = 70 },
                EquipmentType.OilPump => new OilPumpModel { Name = $"油泵-{Elements.Count + 1}", Position = new Point(100, 100), Width = 90, Height = 90 },
                EquipmentType.SideBlowValve => new SideBlowValveModel { Name = $"侧吹阀-{Elements.Count + 1}", Position = new Point(100, 100), Width = 60, Height = 80 },
                EquipmentType.ThreeWayValve => new ThreeWayValveModel { Name = $"三通阀-{Elements.Count + 1}", Position = new Point(100, 100), Width = 50, Height = 80 },
                EquipmentType.TwoWayValve => new TwoWayValveModel { Name = $"两通阀-{Elements.Count + 1}", Position = new Point(100, 100), Width = 50, Height = 80 },
                EquipmentType.PipeReducer => new PipeReducerModel { Name = $"变径管-{Elements.Count + 1}", Position = new Point(100, 100), Width = 150, Height = 110 },
                EquipmentType.SideBlowDevice => new SideBlowDeviceModel { Name = $"侧吹装置-{Elements.Count + 1}", Position = new Point(100, 100), Width = 300, Height = 180 },
                _ => new RootBlowerModel { Name = $"设备-{Elements.Count + 1}", Position = new Point(100, 100), Width = 100, Height = 100 }
            };
        }

        private BaseEquipmentControl CreateEquipmentControl(EquipmentBase model)
        {
            BaseEquipmentControl ctrl = model.Type switch
            {
                EquipmentType.RootBlower => new BlowerControl { EquipmentModel = model },
                EquipmentType.DustCollector => new DustCollectorControl { EquipmentModel = model },
                EquipmentType.FeedBin => new FeedBinControl { EquipmentModel = model },
                EquipmentType.PressurizedTank => new PressurizedTankControl { EquipmentModel = model },
                EquipmentType.CarbonBlackTank => new TankControl { EquipmentModel = model, TankType = Models.TankType.CarbonBlack },
                EquipmentType.OilTank => new TankControl { EquipmentModel = model, TankType = Models.TankType.Oil },
                EquipmentType.CarbonBlackScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.CarbonBlack },
                EquipmentType.OilScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.Oil },
                EquipmentType.Mixer => new MixerControl { EquipmentModel = model },
                EquipmentType.RubberScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.Rubber },
                EquipmentType.RubberConveyor => new ConveyorControl { EquipmentModel = model },
                EquipmentType.OilPump => new OilPumpControl { EquipmentModel = model },
                EquipmentType.SideBlowValve => new SideBlowValveControl { EquipmentModel = model },
                EquipmentType.ThreeWayValve => new ValveControl { EquipmentModel = model, ValveType = Models.ValveType.YType },
                EquipmentType.TwoWayValve => new ValveControl { EquipmentModel = model, ValveType = Models.ValveType.VType },
                EquipmentType.PipeReducer => new PipeReducerControl { EquipmentModel = model },
                EquipmentType.SideBlowDevice => new SideBlowDeviceControl { EquipmentModel = model },
                _ => null
            };

            if (ctrl != null)
            {
                ctrl.Selected += OnElementSelected;
                ctrl.Deselected += OnElementDeselected;
            }

            return ctrl;
        }

        private void OnElementSelected(BaseEquipmentControl ctrl)
        {
            SelectedElement = ctrl;
        }

        private void OnElementDeselected(BaseEquipmentControl ctrl)
        {
            if (SelectedElement == ctrl)
                SelectedElement = null;
        }

        public PipeSegment CreatePipe(AnchorPoint from, AnchorPoint to)
        {
            var pipe = new StraightPipe
            {
                StartPoint = from.Position,
                EndPoint = to.Position,
                Diameter = 50,
                PipeType = PipeType.CarbonConveying,
                StartAnchorId = from.Id,
                EndAnchorId = to.Id
            };

            Pipes.Add(pipe);
            return pipe;
        }

        #endregion

        #region 日志与树形图

        public void AddLog(string message)
        {
            if (_outputList != null)
            {
                _outputList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                _outputList.ScrollIntoView(_outputList.Items[_outputList.Items.Count - 1]);
            }
        }

        private void RebuildObjectTree()
        {
            ProjectElements.Clear();
            foreach (var elem in Elements)
            {
                var node = new TreeNode
                {
                    Name = elem.Name,
                    Type = elem.Type.ToString(),
                    Icon = "📦",
                    Tag = elem
                };
                ProjectElements.Add(node);
            }
        }

        #endregion

        #region 撤销/重做

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoStack.Pop();
            if (cmd is UndoableCommand undoCmd)
                undoCmd.Undo();
            _redoStack.Push(cmd);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            IsDirty = true;
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoStack.Pop();
            cmd.Execute(null);
            _undoStack.Push(cmd);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            IsDirty = true;
        }

        private void AddToUndoStack(ICommand command)
        {
            _undoStack.Push(command);
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion

        private void DeleteSelectedElement()
        {
            if (SelectedElement == null) return;
            var element = SelectedElement;
            var model = element.EquipmentModel;
            
            var cmd = new UndoableCommand(
                () => { Elements.Remove(element); _project.Equipments.Remove(model); DeselectAll(); RebuildObjectTree(); },
                () => { Elements.Add(element); _project.Equipments.Add(model); RebuildObjectTree(); });
            
            cmd.Execute(null);
            AddToUndoStack(cmd);
        }

        public void SwitchToRuntime()
        {
            // 切换到运行时模式
            MessageBox.Show("切换到运行时模式 - 待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenTagBindingDialog(EquipmentBase model)
        {
            var dialog = new TagBindingDialog(model.TagBindings ?? new Dictionary<string, string>(), model);
            if (dialog.ShowDialog() == true)
            {
                model.TagBindings = dialog.TagBindings
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                    .ToDictionary(x => x.Key, x => x.Value);
                IsDirty = true;
                UpdatePropertyPanel();
                AddLog($"更新标签绑定: {model.Name}");
            }
        }

        #region 属性面板

        private void UpdatePropertyPanel()
        {
            if (_propertyPanelControl == null) return;

            _propertyPanelControl.Children.Clear();

            if (SelectedElement == null || SelectedElement.EquipmentModel == null)
            {
                _propertyPanelControl.Children.Add(new TextBlock
                {
                    Text = "未选择元素",
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            var model = SelectedElement.EquipmentModel;

            // 基本信息
            AddPropertyRow("名称", model.Name);
            AddPropertyRow("类型", model.Type.ToString());
            AddPropertyRow("ID", model.Id.Substring(0, 8) + "...");

            // 位置和大小
            AddPropertyRow("X", model.Position.X.ToString("F1"));
            AddPropertyRow("Y", model.Position.Y.ToString("F1"));
            AddPropertyRow("宽度", model.Width.ToString("F1"));
            AddPropertyRow("高度", model.Height.ToString("F1"));
            AddPropertyRow("旋转", model.Rotation.ToString("F1") + "°");

            // 状态
            AddPropertyRow("状态", model.State.ToString());

            // 标签绑定按钮
            var tagBindingButton = new Button
            {
                Content = "🔗 标签绑定配置",
                Background = (Brush)new BrushConverter().ConvertFrom("#3A3A3A"),
                Foreground = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#555555"),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            tagBindingButton.Click += (s, e) => OpenTagBindingDialog(model);
            _propertyPanelControl.Children.Add(tagBindingButton);

            // 设备特定属性
            switch (model)
            {
                case RootBlowerModel blower:
                    AddPropertyRow("转速", $"{blower.RotorSpeed:F0} RPM");
                    AddPropertyRow("叶轮数", blower.ImpellerCount.ToString());
                    AddPropertyRow("电机状态", blower.MotorState.ToString());
                    break;
                case FeedBinModel bin:
                    AddPropertyRow("门状态", bin.DoorState.ToString());
                    AddPropertyRow("填充率", $"{bin.FillRatio:F1}%");
                    AddPropertyRow("粉尘级别", $"{bin.DustLevel:F1}%");
                    break;
                case PressurizedTankModel tank:
                    AddPropertyRow("填充率", $"{tank.FillRatio:F1}%");
                    AddPropertyRow("进料阀", tank.InletValveOpen ? "开" : "关");
                    AddPropertyRow("出料阀", tank.OutletValveOpen ? "开" : "关");
                    break;
                case CarbonBlackTankModel cbTank:
                    AddPropertyRow("填充率", $"{cbTank.FillRatio:F1}%");
                    AddPropertyRow("温度", $"{cbTank.Temperature:F1}°C");
                    break;
                case OilTankModel oilTank:
                    AddPropertyRow("填充率", $"{oilTank.FillRatio:F1}%");
                    AddPropertyRow("温度", $"{oilTank.Temperature:F1}°C");
                    break;
                case MixerModel mixer:
                    AddPropertyRow("进料门", mixer.FeedDoorState.ToString());
                    AddPropertyRow("出料门", mixer.DischargeDoorState.ToString());
                    AddPropertyRow("转速", $"{mixer.Speed:F0} RPM");
                    AddPropertyRow("温度", $"{mixer.Temperature:F1}°C");
                    AddPropertyRow("压力", $"{mixer.Pressure:F2} MPa");
                    AddPropertyRow("功率", $"{mixer.Power:F1} kW");
                    break;
                case OilPumpModel pump:
                    AddPropertyRow("转速", $"{pump.RotorSpeed:F0} RPM");
                    AddPropertyRow("流向", pump.FlowDirection.ToString());
                    AddPropertyRow("电机状态", pump.MotorState.ToString());
                    break;
                case RubberConveyorModel rb:
                    AddPropertyRow("皮带速度", $"{rb.BeltSpeed:F1} m/s");
                    AddPropertyRow("电机状态", rb.MotorState.ToString());
                    break;
                case DustCollectorModel dcm:
                    AddPropertyRow("电机状态", dcm.MotorState.ToString());
                    break;
            }
        }

        private void AddPropertyRow(string name, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            panel.Children.Add(new TextBlock
            {
                Text = name + ": ",
                Foreground = Brushes.LightGray,
                FontSize = 11,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            _propertyPanelControl.Children.Add(panel);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}