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
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Controls;
using TwoD5DMonitoring.Controls.Equipment;
using TwoD5DMonitoring.Controls.Pipes;

namespace TwoD5DMonitoring.Designer
{
    public partial class DesignerCanvas : UserControl, INotifyPropertyChanged
    {
        private Point _dragStart;
        private Point _elementStart;
        private bool _isDragging;
        private BaseEquipmentControl _draggedElement;
        private PipeControl _draggedPipe;
        private AnchorPoint _connectionStartAnchor;
        private BaseEquipmentControl _connectionStartElement;
        private bool _isCreatingConnection;
        private Point _lastMousePos;

        // 缺失的属性
        public bool SnapToGrid { get; set; } = true;
        public double GridSize { get; set; } = 20.0;

        public DesignerCanvas()
        {
            InitializeComponent();
            DataContext = _viewModel = new DesignerViewModel();
            Loaded += OnLoaded;
        }

        private DesignerViewModel _viewModel;

        public DesignerViewModel ViewModel => _viewModel;

        // Log方法
        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[DesignerCanvas] {message}");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Initialize(DesignCanvas, PipeLinesControl, ElementsControl, ObjectTree, OutputList, PropertyPanelControl);
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DesignerViewModel.ShowGrid))
                    GridCanvas.Visibility = _viewModel.ShowGrid ? Visibility.Visible : Visibility.Collapsed;
                if (e.PropertyName == nameof(DesignerViewModel.GridSize))
                    UpdateGrid();
            };
        }

        private void UpdateGrid()
        {
            // 网格大小变化时重绘
        }

        #region 画布交互

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(DesignCanvas);
            Focus();
            DesignCanvas.Focus();
            
            if (e.RightButton == MouseButtonState.Pressed)
            {
                // 右键菜单
                return;
            }

            // 如果在创建连接模式
            if (_isCreatingConnection)
            {
                CancelConnection();
                return;
            }

            // 检查是否点击了锚点
            var hit = VisualTreeHelper.HitTest(DesignCanvas, _lastMousePos);
            if (hit?.VisualHit is FrameworkElement fe && fe.Tag is AnchorPoint anchor)
            {
                StartConnection(anchor);
                e.Handled = true;
                return;
            }

            // 如果点击空白处，取消选择
            if (hit?.VisualHit == DesignCanvas || hit?.VisualHit == GridCanvas)
            {
                _viewModel.DeselectAll();
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(DesignCanvas);
            CoordText.Text = $"X: {(int)pos.X}, Y: {(int)pos.Y}";

            if (_isCreatingConnection)
            {
                // 更新预览连接线
                PreviewLineGeometry.StartPoint = _connectionStartAnchor.Position;
                PreviewLineGeometry.EndPoint = pos;
                PreviewConnectionLine.Visibility = Visibility.Visible;
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isCreatingConnection)
            {
                var hit = VisualTreeHelper.HitTest(DesignCanvas, e.GetPosition(DesignCanvas));
                if (hit?.VisualHit is FrameworkElement fe && fe.Tag is AnchorPoint targetAnchor)
                {
                    if (targetAnchor != _connectionStartAnchor && 
                        targetAnchor.OwnerElementId != _connectionStartAnchor.OwnerElementId)
                    {
                        CreateConnection(_connectionStartAnchor, targetAnchor);
                    }
                }
                CancelConnection();
            }
        }

        private void OnCanvasRightClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            // 显示右键菜单
        }

        private void OnCanvasDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolboxItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnCanvasDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolboxItem"))
            {
                var tool = e.Data.GetData("ToolboxItem") as ToolboxItem;
                if (tool != null)
                {
                    var pos = e.GetPosition(DesignCanvas);
                    if (SnapToGrid)
                    {
                        pos.X = Math.Round(pos.X / GridSize) * GridSize;
                        pos.Y = Math.Round(pos.Y / GridSize) * GridSize;
                    }
                    _viewModel.CreateElement(tool.Type, pos);
                }
                e.Handled = true;
            }
        }

        #endregion

        #region 连接管理

        private void StartConnection(AnchorPoint anchor)
        {
            if (anchor.IsConnected) return;

            _connectionStartAnchor = anchor;
            _connectionStartElement = FindElementById(anchor.OwnerElementId);
            _isCreatingConnection = true;
            PreviewConnectionLine.Visibility = Visibility.Visible;
            
            Log($"开始创建连接: {anchor.Name}");
        }

        private void CancelConnection()
        {
            _isCreatingConnection = false;
            _connectionStartAnchor = null;
            _connectionStartElement = null;
            PreviewConnectionLine.Visibility = Visibility.Collapsed;
        }

        private void CreateConnection(AnchorPoint fromAnchor, AnchorPoint toAnchor)
        {
            var pipe = _viewModel.CreatePipe(fromAnchor, toAnchor);
            if (pipe != null)
            {
                fromAnchor.IsConnected = true;
                fromAnchor.ConnectedPipeId = pipe.Id;
                toAnchor.IsConnected = true;
                toAnchor.ConnectedPipeId = pipe.Id;
                
                _viewModel.AddLog($"已连接: {fromAnchor.Name} → {toAnchor.Name}");
            }
        }

        private BaseEquipmentControl FindElementById(string id)
        {
            foreach (var presenter in ElementsControl.Items)
            {
                if (presenter is ContentPresenter cp && cp.Content is BaseEquipmentControl ctrl)
                {
                    if (ctrl.Id == id) return ctrl;
                }
            }
            return null;
        }

        #endregion

        #region 工具栏命令

        private void OnNewProject(object sender, RoutedEventArgs e) => _viewModel.NewProject();
        private void OnOpenProject(object sender, RoutedEventArgs e) => _viewModel.OpenProject();
        private void OnSaveProject(object sender, RoutedEventArgs e) => _viewModel.SaveProject();
        private void OnUndo(object sender, RoutedEventArgs e) => _viewModel.Undo();
        private void OnRedo(object sender, RoutedEventArgs e) => _viewModel.Redo();
        private void OnToggleGrid(object sender, RoutedEventArgs e) => _viewModel.ShowGrid = !_viewModel.ShowGrid;
        private void OnToggleSnap(object sender, RoutedEventArgs e) => _viewModel.SnapToGrid = !_viewModel.SnapToGrid;
        private void OnSwitchToRuntime(object sender, RoutedEventArgs e) => _viewModel.SwitchToRuntime();

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}