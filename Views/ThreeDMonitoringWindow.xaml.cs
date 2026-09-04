using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Serilog;
using TwoD5DMonitoring.Infrastructure._3D;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;

namespace TwoD5DMonitoring.Views
{
    public partial class ThreeDMonitoringWindow : Window
    {
        private readonly ILogger _logger;
        private readonly I3DSceneManager _sceneManager;
        private readonly PlcManager? _plcManager;
        private DispatcherTimer? _updateTimer;
        private bool _isLoading = false;

        public ThreeDMonitoringWindow(I3DSceneManager? sceneManager = null, PlcManager? plcManager = null)
        {
            InitializeComponent();
            
            _logger = LogManager.CreateModuleLogger(nameof(ThreeDMonitoringWindow));
            _sceneManager = sceneManager ?? new ThreeDSceneManager();
            _plcManager = plcManager;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Information("3D Monitoring Window Loaded");
                
                // 初始化场景管理器
                var initResult = await _sceneManager.InitializeAsync();
                if (!initResult)
                {
                    UpdateStatus("场景管理器初始化失败");
                    return;
                }

                // 加载默认场景
                await LoadDefaultSceneAsync();

                // 订阅事件
                _sceneManager.DeviceStatusUpdated += OnDeviceStatusUpdated;
                _sceneManager.SceneLoaded += OnSceneLoaded;
                _sceneManager.ErrorOccurred += OnSceneError;

                // 如果有 PLC 管理器，订阅数据变化
                if (_plcManager != null)
                {
                    _plcManager.AnyValueChanged += OnPlcValueChanged;
                }

                // 启动 UI 更新定时器
                _updateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1000 / 60) // 60 FPS
                };
                _updateTimer.Tick += OnUpdateTimer;
                _updateTimer.Start();

                UpdateStatus("3D 监控已就绪 (预览版)");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading 3D monitoring window");
                UpdateStatus($"错误: {ex.Message}");
            }
        }

        private async void OnClosed(object sender, EventArgs e)
        {
            _updateTimer?.Stop();
            
            _sceneManager.DeviceStatusUpdated -= OnDeviceStatusUpdated;
            _sceneManager.SceneLoaded -= OnSceneLoaded;
            _sceneManager.ErrorOccurred -= OnSceneError;
            
            if (_plcManager != null)
            {
                _plcManager.AnyValueChanged -= OnPlcValueChanged;
            }

            await _sceneManager.ShutdownAsync();
        }

        private async Task LoadDefaultSceneAsync()
        {
            SetLoading(true);
            try
            {
                var result = await _sceneManager.LoadSceneAsync("default");
                if (result)
                {
                    UpdateDeviceList();
                    UpdateStatus("默认场景加载完成");
                }
                else
                {
                    UpdateStatus("场景加载失败");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load default scene");
                UpdateStatus($"加载失败: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void UpdateDeviceList()
        {
            var devices = _sceneManager.GetAllDevicesAsync().Result;
            DeviceListControl.ItemsSource = devices;
            DeviceCountText.Text = $"设备: {devices.Count()}";
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }

        private void SetLoading(bool loading)
        {
            Dispatcher.Invoke(() =>
            {
                _isLoading = loading;
                LoadingOverlay.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
                LoadSceneButton.IsEnabled = !loading;
                SaveSceneButton.IsEnabled = !loading;
            });
        }

        private void OnDeviceStatusUpdated(object? sender, DeviceStatusUpdate e)
        {
            // 可以在这里处理设备状态更新，比如触发动画
            Dispatcher.Invoke(() =>
            {
                // 更新 UI 中的设备状态显示
            });
        }

        private void OnSceneLoaded(object? sender, string sceneId)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"场景已加载: {sceneId}");
                UpdateDeviceList();
                
                // 更新场景下拉框
                if (!SceneComboBox.Items.Contains(sceneId))
                {
                    SceneComboBox.Items.Add(sceneId);
                }
                SceneComboBox.SelectedItem = sceneId;
            });
        }

        private void OnSceneError(object? sender, string error)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"错误: {error}");
                MessageBox.Show($"3D 场景错误: {error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnPlcValueChanged(object? sender, PlcValueChangedEventArgs e)
        {
            // 将 PLC 数据映射到 3D 设备
            // 这里需要根据实际的标签映射配置来实现
            Dispatcher.Invoke(async () =>
            {
                // 示例：如果标签名匹配设备 ID，更新设备状态
                var device = await _sceneManager.GetDeviceAsync(e.TagName);
                if (device != null)
                {
                    await _sceneManager.UpdateDeviceStatusAsync(new DeviceStatusUpdate
                    {
                        DeviceId = e.TagName,
                        PropertyName = "Value",
                        Value = e.NewValue,
                        Timestamp = e.Timestamp,
                        Quality = e.Quality
                    });
                }
            });
        }

        private void OnUpdateTimer(object? sender, EventArgs e)
        {
            // 更新 FPS 显示等
            Dispatcher.Invoke(() =>
            {
                FpsText.Text = "FPS: 60 (模拟)";
            });
        }

        // UI 事件处理
        private async void SceneComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SceneComboBox.SelectedItem is string sceneId && !_isLoading)
            {
                SetLoading(true);
                try
                {
                    await _sceneManager.LoadSceneAsync(sceneId);
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private async void LoadSceneButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "3D Scene Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "加载 3D 场景"
            };

            if (dialog.ShowDialog() == true)
            {
                SetLoading(true);
                try
                {
                    var json = System.IO.File.ReadAllText(dialog.FileName);
                    await _sceneManager.LoadSceneFromJsonAsync(json);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to load scene from file");
                    UpdateStatus($"加载失败: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private async void SaveSceneButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "3D Scene Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "保存 3D 场景",
                FileName = $"scene_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                SetLoading(true);
                try
                {
                    await _sceneManager.SaveSceneAsync(dialog.FileName);
                    UpdateStatus($"场景已保存: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to save scene to file");
                    UpdateStatus($"保存失败: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private void EditSceneButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ThreeDEditorWindow(_sceneManager);
            editor.Owner = this;
            editor.ShowDialog();
        }

        private async void ResetCameraButton_Click(object sender, RoutedEventArgs e)
        {
            await _sceneManager.SetCameraPositionAsync(0, 10, 20, 0, 0, 0);
            UpdateStatus("视角已重置");
        }

        private void ToggleGridButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("网格显示切换 (预留功能)");
        }

        private async void StartAllAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            var devices = await _sceneManager.GetAllDevicesAsync();
            foreach (var device in devices)
            {
                await _sceneManager.StartAnimationAsync(device.DeviceId, "Idle", new Dictionary<string, object> { { "speed", 1.0 } });
            }
            UpdateStatus("所有设备动画已启动");
        }

        private async void StopAllAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            var devices = await _sceneManager.GetAllDevicesAsync();
            foreach (var device in devices)
            {
                await _sceneManager.StopAnimationAsync(device.DeviceId, "Idle");
            }
            UpdateStatus("所有设备动画已停止");
        }
    }
}