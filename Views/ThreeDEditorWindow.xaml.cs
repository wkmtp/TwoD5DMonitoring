using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;
using TwoD5DMonitoring.Infrastructure._3D;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Views
{
    public partial class ThreeDEditorWindow : Window
    {
        private readonly ILogger _logger;
        private readonly I3DSceneManager _sceneManager;
        private DeviceTreeItem? _selectedDevice;
        private bool _isPreviewMode = false;

        public ThreeDEditorWindow(I3DSceneManager? sceneManager = null)
        {
            InitializeComponent();

            _logger = LogManager.CreateModuleLogger(nameof(ThreeDEditorWindow));
            _sceneManager = sceneManager ?? new ThreeDSceneManager();

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Information("3D Editor Window Loaded");
                await InitializeEditorAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading 3D editor window");
                UpdateStatus($"错误: {ex.Message}");
            }
        }

        private async void OnClosed(object sender, EventArgs e)
        {
            await _sceneManager.ShutdownAsync();
        }

        private async Task InitializeEditorAsync()
        {
            UpdateStatus("初始化编辑器...");
            var initResult = await _sceneManager.InitializeAsync();
            if (!initResult)
            {
                UpdateStatus("场景管理器初始化失败");
                return;
            }

            await LoadDefaultSceneAsync();
            PopulateDeviceLibrary();
            DrawGrid();
            UpdateStatus("3D 编辑器已就绪");
        }

        private async Task LoadDefaultSceneAsync()
        {
            var result = await _sceneManager.LoadSceneAsync("default");
            if (result)
            {
                UpdateDeviceHierarchy();
                UpdateStatus("默认场景加载完成");
            }
            else
            {
                UpdateStatus("场景加载失败");
            }
        }

        private void PopulateDeviceLibrary()
        {
            var deviceTypes = new[]
            {
                new { Name = "油泵", Type = "OilPump", Icon = "⛽", Category = "流体设备" },
                new { Name = "阀门", Type = "Valve", Icon = "🔧", Category = "流体设备" },
                new { Name = "储罐", Type = "Tank", Icon = "🛢️", Category = "容器设备" },
                new { Name = "压力罐", Type = "PressurizedTank", Icon = "⚡", Category = "容器设备" },
                new { Name = "混合器", Type = "Mixer", Icon = "🌀", Category = "工艺设备" },
                new { Name = "吹风机", Type = "Blower", Icon = "💨", Category = "气动设备" },
                new { Name = "输送带", Type = "Conveyor", Icon = "📦", Category = "输送设备" },
                new { Name = "除尘器", Type = "DustCollector", Icon = "🌪️", Category = "环保设备" },
                new { Name = "进料仓", Type = "FeedBin", Icon = "📥", Category = "储存设备" },
                new { Name = "侧吹装置", Type = "SideBlowDevice", Icon = "💨", Category = "工艺设备" },
                new { Name = "侧吹阀", Type = "SideBlowValve", Icon = "🔧", Category = "流体设备" },
                new { Name = "称重秤", Type = "Scale", Icon = "⚖️", Category = "计量设备" },
                new { Name = "管道缩径", Type = "PipeReducer", Icon = "🔗", Category = "管道配件" },
            };

            foreach (var group in deviceTypes.GroupBy(d => d.Category))
            {
                var groupHeader = new TextBlock
                {
                    Text = group.Key,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                    Margin = new Thickness(0, 10, 0, 5),
                    FontSize = 12
                };
                DeviceLibraryPanel.Children.Add(groupHeader);

                foreach (var device in group)
                {
                    var btn = new Button
                    {
                        Content = $"{device.Icon} {device.Name}",
                        Tag = device.Type,
                        Margin = new Thickness(0, 2, 0, 2),
                        Padding = new Thickness(10, 5, 10, 5),
                        Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                        BorderThickness = new Thickness(1),
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        FontSize = 12
                    };
                    btn.Click += DeviceLibraryItem_Click;
                    DeviceLibraryPanel.Children.Add(btn);
                }
            }
        }

        private void DeviceLibraryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string deviceType)
            {
                AddDeviceToScene(deviceType);
            }
        }

        private void AddDeviceToScene(string deviceType)
        {
            var deviceId = $"{deviceType}_{Guid.NewGuid().ToString()[..8]}";
            var device3D = new Device3DInfo
            {
                DeviceId = deviceId,
                DeviceName = deviceType,
                ModelType = deviceType,
                PositionX = 0,
                PositionY = 0,
                PositionZ = 0,
                ScaleX = 1,
                ScaleY = 1,
                ScaleZ = 1
            };

            var task = _sceneManager.AddDeviceAsync(device3D);
            task.Wait();

            var treeItem = new DeviceTreeItem
            {
                Name = deviceType,
                Icon = GetDeviceIcon(deviceType),
                DeviceId = deviceId,
                IsVisible = true
            };
            SceneHierarchyTree.Items.Add(treeItem);

            UpdateStatus($"已添加设备: {deviceType} ({deviceId})");
            UpdateSceneInfo();
        }

        private string GetDeviceIcon(string deviceType)
        {
            return deviceType switch
            {
                "OilPump" => "⛽",
                "Valve" => "🔧",
                "Tank" => "🛢️",
                "PressurizedTank" => "⚡",
                "Mixer" => "🌀",
                "Blower" => "💨",
                "Conveyor" => "📦",
                "DustCollector" => "🌪️",
                "FeedBin" => "📥",
                "SideBlowDevice" => "💨",
                "SideBlowValve" => "🔧",
                "Scale" => "⚖️",
                "PipeReducer" => "🔗",
                _ => "⬜"
            };
        }

        private void UpdateDeviceHierarchy()
        {
            SceneHierarchyTree.Items.Clear();
            var devices = _sceneManager.GetAllDevicesAsync().Result;
            foreach (var device in devices)
            {
                var treeItem = new DeviceTreeItem
                {
                    Name = device.DeviceName,
                    Icon = GetDeviceIcon(device.ModelType),
                    DeviceId = device.DeviceId,
                    IsVisible = true
                };
                SceneHierarchyTree.Items.Add(treeItem);
            }
            UpdateSceneInfo();
        }

        private void UpdateSceneInfo()
        {
            var count = SceneHierarchyTree.Items.Count;
            SceneInfoText.Text = $"场景: 未保存 | 设备: {count}";
        }

        private void DrawGrid()
        {
            var canvas = GridCanvas;
            canvas.Children.Clear();
            
            var gridSize = 20.0;
            var canvasSize = 300.0;
            var halfSize = canvasSize / 2;
            var lineColor = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            var axisColor = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));

            for (int i = -10; i <= 10; i++)
            {
                var offset = i * gridSize;
                
                var hLine = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = halfSize + offset,
                    X2 = canvasSize, Y2 = halfSize + offset,
                    Stroke = lineColor,
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(hLine);

                var vLine = new System.Windows.Shapes.Line
                {
                    X1 = halfSize + offset, Y1 = 0,
                    X2 = halfSize + offset, Y2 = canvasSize,
                    Stroke = lineColor,
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(vLine);
            }

            var xAxis = new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = halfSize,
                X2 = canvasSize, Y2 = halfSize,
                Stroke = axisColor,
                StrokeThickness = 1.5
            };
            canvas.Children.Add(xAxis);

            var yAxis = new System.Windows.Shapes.Line
            {
                X1 = halfSize, Y1 = 0,
                X2 = halfSize, Y2 = canvasSize,
                Stroke = axisColor,
                StrokeThickness = 1.5
            };
            canvas.Children.Add(yAxis);
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                EditorStatusText.Text = message;
            });
        }

        // ===== 事件处理器 =====

        private void NewScene_Click(object sender, RoutedEventArgs e)
        {
            SceneHierarchyTree.Items.Clear();
            DeviceLibraryPanel.Children.Clear();
            PopulateDeviceLibrary();
            _sceneManager.LoadSceneAsync("default").Wait();
            UpdateStatus("已创建新场景");
        }

        private void OpenScene_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "3D Scene Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "打开 3D 场景"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    _sceneManager.LoadSceneFromJsonAsync(json).Wait();
                    UpdateDeviceHierarchy();
                    UpdateStatus($"已打开场景: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to open scene");
                    MessageBox.Show($"打开场景失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveScene_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "3D Scene Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "保存 3D 场景",
                FileName = $"scene_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _sceneManager.SaveSceneAsync(dialog.FileName).Wait();
                    UpdateStatus($"场景已保存: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to save scene");
                    MessageBox.Show($"保存场景失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAsScene_Click(object sender, RoutedEventArgs e)
        {
            SaveScene_Click(sender, e);
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            // 打开设备类型选择对话框或直接添加默认设备
            AddDeviceToScene("Tank");
        }

        private void DeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice != null)
            {
                _sceneManager.RemoveDeviceAsync(_selectedDevice.DeviceId).Wait();
                SceneHierarchyTree.Items.Remove(_selectedDevice);
                _selectedDevice = null;
                PropertyPanel.Children.Clear();
                PropertyPanel.Children.Add(new TextBlock
                {
                    Text = "请选择一个设备",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                UpdateStatus($"已删除设备");
                UpdateSceneInfo();
            }
        }

        private void DuplicateDevice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice != null)
            {
                var originalId = _selectedDevice.DeviceId;
                var newId = $"{originalId}_copy_{Guid.NewGuid().ToString()[..4]}";
                // 简化实现：只是在层级中复制
                var treeItem = new DeviceTreeItem
                {
                    Name = _selectedDevice.Name + " (副本)",
                    Icon = _selectedDevice.Icon,
                    DeviceId = newId,
                    IsVisible = true
                };
                SceneHierarchyTree.Items.Add(treeItem);
                UpdateStatus($"已复制设备: {newId}");
                UpdateSceneInfo();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("撤销 (预留功能)");
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("重做 (预留功能)");
        }

        private void PreviewMode_Click(object sender, RoutedEventArgs e)
        {
            _isPreviewMode = !_isPreviewMode;
            if (sender is Button btn)
            {
                btn.Content = _isPreviewMode ? "🔧 编辑模式" : "👁 预览模式";
                btn.Background = _isPreviewMode ? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)) : new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2));
            }
            UpdateStatus(_isPreviewMode ? "已切换到预览模式" : "已切换到编辑模式");
        }

        private void HierarchySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = HierarchySearchBox.Text?.ToLower() ?? "";
            // 简化实现：只过滤显示
            foreach (var item in SceneHierarchyTree.Items)
            {
                if (item is DeviceTreeItem treeItem)
                {
                    var matches = string.IsNullOrEmpty(searchText) || 
                                  treeItem.Name.ToLower().Contains(searchText);
                    treeItem.IsVisible = matches;
                }
            }
        }

        private void SceneHierarchyTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedDevice = e.NewValue as DeviceTreeItem;
            if (_selectedDevice != null)
            {
                SelectedDeviceText.Text = $"选中: {_selectedDevice.Name} ({_selectedDevice.DeviceId})";
                ShowDeviceProperties(_selectedDevice);
            }
            else
            {
                SelectedDeviceText.Text = "未选择设备";
            }
        }

        private void ShowDeviceProperties(DeviceTreeItem device)
        {
            PropertyPanel.Children.Clear();
            
            var nameLabel = new TextBlock
            {
                Text = $"设备: {device.Name}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            PropertyPanel.Children.Add(nameLabel);

            var idLabel = new TextBlock
            {
                Text = $"ID: {device.DeviceId}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            PropertyPanel.Children.Add(idLabel);

            var visibleCheck = new CheckBox
            {
                Content = "可见",
                IsChecked = device.IsVisible,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 5, 0, 5)
            };
            visibleCheck.Checked += (s, e) => device.IsVisible = true;
            visibleCheck.Unchecked += (s, e) => device.IsVisible = false;
            PropertyPanel.Children.Add(visibleCheck);

            // 位置属性
            AddPropertyField("位置 X", "0", PropertyPanel);
            AddPropertyField("位置 Y", "0", PropertyPanel);
            AddPropertyField("位置 Z", "0", PropertyPanel);
            AddPropertyField("旋转 X", "0", PropertyPanel);
            AddPropertyField("旋转 Y", "0", PropertyPanel);
            AddPropertyField("旋转 Z", "0", PropertyPanel);
            AddPropertyField("缩放 X", "1", PropertyPanel);
            AddPropertyField("缩放 Y", "1", PropertyPanel);
            AddPropertyField("缩放 Z", "1", PropertyPanel);
        }

        private void AddPropertyField(string label, string value, Panel panel)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            sp.Children.Add(new TextBlock { Text = label + ":", Width = 60, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)) });
            sp.Children.Add(new TextBox { Text = value, Width = 80, Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), BorderThickness = new Thickness(1), FontSize = 11 });
            panel.Children.Add(sp);
        }

        private async void ExportThreeJs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "导出为 Three.js 场景",
                FileName = $"scene_threejs_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = await _sceneManager.SaveSceneToJsonAsync();
                    File.WriteAllText(dialog.FileName, json);
                    UpdateStatus($"已导出 Three.js 场景: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to export Three.js scene");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportUnity_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("导出 Unity Prefab (预留功能)");
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "导出设备列表",
                FileName = $"devices_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var devices = _sceneManager.GetAllDevicesAsync().Result;
                    var lines = new List<string> { "DeviceId,DeviceName,ModelType,PositionX,PositionY,PositionZ,ScaleX,ScaleY,ScaleZ" };
                    foreach (var d in devices)
                    {
                        lines.Add($"{d.DeviceId},{d.DeviceName},{d.ModelType},{d.PositionX},{d.PositionY},{d.PositionZ},{d.ScaleX},{d.ScaleY},{d.ScaleZ}");
                    }
                    File.WriteAllLines(dialog.FileName, lines);
                    UpdateStatus($"已导出 CSV: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to export CSV");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class DeviceTreeItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public bool IsVisible { get; set; } = true;
        public List<DeviceTreeItem> Children { get; set; } = new();
    }
}