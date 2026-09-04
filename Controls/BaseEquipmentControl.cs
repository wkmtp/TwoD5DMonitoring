using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Extensions;
using TwoD5DMonitoring.Infrastructure;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Controls
{
    public abstract class BaseEquipmentControl : UserControl, INotifyPropertyChanged, IDisposable
    {

        #region 依赖属性

        public static readonly DependencyProperty EquipmentModelProperty =
            DependencyProperty.Register(nameof(EquipmentModel), typeof(EquipmentBase), typeof(BaseEquipmentControl),
                new PropertyMetadata(null, OnEquipmentModelChanged));

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(BaseEquipmentControl),
                new PropertyMetadata(false, OnIsSelectedChanged));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(BaseEquipmentControl),
                new PropertyMetadata(false, OnIsEditingChanged));

        public static readonly DependencyProperty ShowAnchorsProperty =
            DependencyProperty.Register(nameof(ShowAnchors), typeof(bool), typeof(BaseEquipmentControl),
                new PropertyMetadata(false, OnShowAnchorsChanged));

        public static readonly DependencyProperty SnapToGridProperty =
            DependencyProperty.Register(nameof(SnapToGrid), typeof(bool), typeof(BaseEquipmentControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty GridSizeProperty =
            DependencyProperty.Register(nameof(GridSize), typeof(double), typeof(BaseEquipmentControl),
                new PropertyMetadata(20.0));

        public EquipmentBase EquipmentModel
        {
            get => (EquipmentBase)GetValue(EquipmentModelProperty);
            set => SetValue(EquipmentModelProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public bool ShowAnchors
        {
            get => (bool)GetValue(ShowAnchorsProperty);
            set => SetValue(ShowAnchorsProperty, value);
        }

        public bool SnapToGrid
        {
            get => (bool)GetValue(SnapToGridProperty);
            set => SetValue(SnapToGridProperty, value);
        }

        public double GridSize
        {
            get => (double)GetValue(GridSizeProperty);
            set => SetValue(GridSizeProperty, value);
        }

        #endregion

        #region 属性

        public string Id => EquipmentModel?.Id ?? "";
        public string Name => EquipmentModel?.Name ?? "";
        public EquipmentType Type => EquipmentModel?.Type ?? EquipmentType.RootBlower;
        public EquipmentState State => EquipmentModel?.State ?? EquipmentState.Stopped;
        public Color StateColor => EquipmentModel?.StateColor ?? Colors.Gray;
        public ObservableCollection<AnchorPoint> Anchors => EquipmentModel?.Anchors ?? new();

        #endregion

        #region 事件

        public event Action<BaseEquipmentControl> Selected;
        public event Action<BaseEquipmentControl> Deselected;
        public event Action<BaseEquipmentControl, AnchorPoint> AnchorClicked;
        public event Action<BaseEquipmentControl> PositionChanged;

        #endregion

        #region 字段

        private Point _dragStart;
        private Point _elementStart;
        private bool _isDragging;
        private AnchorPoint _hoveredAnchor;
        private readonly List<FrameworkElement> _anchorElements = new();

        #endregion

        protected BaseEquipmentControl()
        {
            Focusable = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (EquipmentModel != null)
            {
                EquipmentModel.PropertyChanged += OnModelPropertyChanged;
                UpdateAnchors();
            }

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (EquipmentModel != null)
                EquipmentModel.PropertyChanged -= OnModelPropertyChanged;

            MouseLeftButtonDown -= OnMouseLeftButtonDown;
            MouseMove -= OnMouseMove;
            MouseLeftButtonUp -= OnMouseLeftButtonUp;
        }

        private static void OnEquipmentModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseEquipmentControl control)
            {
                if (e.OldValue is EquipmentBase oldModel)
                    oldModel.PropertyChanged -= control.OnModelPropertyChanged;

                if (e.NewValue is EquipmentBase newModel)
                    newModel.PropertyChanged += control.OnModelPropertyChanged;

                control.UpdateVisualState();
                control.UpdateAnchors();
            }
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(EquipmentBase.Position):
                        UpdatePosition();
                        break;
                    case nameof(EquipmentBase.Rotation):
                        UpdateRotation();
                        break;
                    case nameof(EquipmentBase.Width):
                    case nameof(EquipmentBase.Height):
                        UpdateSize();
                        UpdateAnchors();
                        break;
                    case nameof(EquipmentBase.State):
                        UpdateVisualState();
                        break;
                    case nameof(EquipmentBase.IsSelected):
                        UpdateSelectionVisual();
                        break;
                }
            }));
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseEquipmentControl control)
            {
                control.UpdateSelectionVisual();
                if (control.IsSelected)
                    control.Selected?.Invoke(control);
                else
                    control.Deselected?.Invoke(control);
            }
        }

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseEquipmentControl control)
            {
                control.UpdateEditingVisual();
            }
        }

        private static void OnShowAnchorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseEquipmentControl control)
            {
                control.UpdateAnchorsVisibility();
            }
        }

        protected virtual void UpdatePosition()
        {
            if (EquipmentModel != null)
            {
                Canvas.SetLeft(this, EquipmentModel.Position.X);
                Canvas.SetTop(this, EquipmentModel.Position.Y);
            }
        }

        protected virtual void UpdateRotation()
        {
            if (EquipmentModel != null)
            {
                RenderTransform = new RotateTransform(EquipmentModel.Rotation, Width / 2, Height / 2);
            }
        }

        protected virtual void UpdateSize()
        {
            Width = EquipmentModel?.Width ?? 100;
            Height = EquipmentModel?.Height ?? 100;
        }

        public virtual void UpdateVisualState() { }

        protected virtual void UpdateSelectionVisual()
        {
            var border = GetTemplateChild("SelectionBorder") as Border;
            if (border != null)
            {
                border.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        protected virtual void UpdateEditingVisual() { }

        protected virtual void UpdateAnchors()
        {
            // 子类实现具体连接点
        }

        protected virtual void UpdateAnchorsVisibility() { }

        public virtual Rect GetBounds()
        {
            return new Rect(0, 0, ActualWidth, ActualHeight);
        }

        #region 交互处理

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                IsEditing = !IsEditing;
                e.Handled = true;
                return;
            }

            Focus();
            _dragStart = e.GetPosition(Parent as UIElement);
            _elementStart = new Point(EquipmentModel?.Position.X ?? 0, EquipmentModel?.Position.Y ?? 0);
            _isDragging = true;
            CaptureMouse();
            IsSelected = true;
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var currentPos = e.GetPosition(Parent as UIElement);
            var delta = currentPos - _dragStart;

            var newX = _elementStart.X + delta.X;
            var newY = _elementStart.Y + delta.Y;

            if (SnapToGrid)
            {
                newX = Math.Round(newX / GridSize) * GridSize;
                newY = Math.Round(newY / GridSize) * GridSize;
            }

            if (EquipmentModel != null)
            {
                EquipmentModel.Position = new Point(newX, newY);
                PositionChanged?.Invoke(this);
            }
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
            {
                // 显示连接点预览
                UpdateAnchorsVisibility();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSelected && !IsEditing)
            {
                UpdateAnchorsVisibility();
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            IsSelected = true;
            e.Handled = true;
        }

        #endregion

        #region 连接点管理

        protected FrameworkElement CreateAnchorVisual(AnchorPoint anchor, Point position)
        {
            var ellipse = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = anchor.IsConnected ? Brushes.LimeGreen : Brushes.DodgerBlue,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                ToolTip = $"{anchor.Name} ({anchor.Type})",
                Cursor = Cursors.Hand,
                Tag = anchor
            };

            Canvas.SetLeft(ellipse, position.X - 7);
            Canvas.SetTop(ellipse, position.Y - 7);

            ellipse.MouseLeftButtonDown += (s, args) =>
            {
                args.Handled = true;
                AnchorClicked?.Invoke(this, anchor);
            };

            ellipse.MouseEnter += (s, args) =>
            {
                _hoveredAnchor = anchor;
                if (ellipse.Fill != Brushes.LimeGreen)
                    ellipse.Fill = Brushes.Orange;
            };

            ellipse.MouseLeave += (s, args) =>
            {
                _hoveredAnchor = null;
                if (ellipse.Fill != Brushes.LimeGreen)
                    ellipse.Fill = Brushes.DodgerBlue;
            };

            return ellipse;
        }

        protected void UpdateAnchorVisual(AnchorPoint anchor, FrameworkElement visual)
        {
            if (visual is Ellipse ellipse)
            {
                ellipse.Fill = anchor.IsConnected ? Brushes.LimeGreen : Brushes.DodgerBlue;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (EquipmentModel != null)
                {
                    EquipmentModel.PropertyChanged -= OnModelPropertyChanged;
                }

                MouseLeftButtonDown -= OnMouseLeftButtonDown;
                MouseMove -= OnMouseMove;
                MouseLeftButtonUp -= OnMouseLeftButtonUp;
                MouseEnter -= OnMouseEnter;
                MouseLeave -= OnMouseLeave;

                _disposed = true;
            }
        }

        ~BaseEquipmentControl()
        {
            Dispose(false);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}