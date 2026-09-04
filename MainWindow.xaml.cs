using System;
using System.Windows;
using TwoD5DMonitoring.Designer;
using TwoD5DMonitoring.Runtime;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.ViewModels;
using TwoD5DMonitoring.Infrastructure.PLC;
using TwoD5DMonitoring.Services;
using TwoD5DMonitoring.Services.Repositories;
using TwoD5DMonitoring.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace TwoD5DMonitoring
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
        {
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            
            InitializeComponent();
            DataContext = _viewModel;
            
            Loaded += OnLoaded;
            Closed += OnClosed;
            ModeTabControl.SelectionChanged += OnModeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化设计器
            DesignerCanvas.Loaded += (s, e) => 
            {
                // 设计器初始化完成
            };

            // 设置运行时
            RuntimeView.Loaded += (s, e) =>
            {
                // 运行时初始化完成
            };

            // 启动状态定时器
            _viewModel.StartStatusTimer();

            // 连接 PLC
            _ = ConnectPlcAsync();
        }

        private async Task ConnectPlcAsync()
        {
            try
            {
                await _serviceProvider.GetRequiredService<PlcManager>().ConnectAllAsync();
            }
            catch (Exception ex)
            {
                // PLC 连接失败不阻塞启动
                System.Diagnostics.Debug.WriteLine($"PLC 连接失败: {ex.Message}");
            }
        }

        private void OnModeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Tab 切换由 ViewModel 的 CurrentTabIndex 绑定处理
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // 清理资源
            _viewModel.StopStatusTimer();
            
            var runtimeView = _serviceProvider.GetService<RuntimeView>();
            runtimeView?.Stop();
        }
    }
}