using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TwoD5DMonitoring.Models;
using ModelsTankType = TwoD5DMonitoring.Models.TankType;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class TankControl : BaseEquipmentControl
    {
        #region 依赖属性

        public static readonly DependencyProperty TankNameProperty =
            DependencyProperty.Register(nameof(TankName), typeof(string), typeof(TankControl),
                new PropertyMetadata("炭黑罐", OnTankNameChanged));

        public static readonly DependencyProperty TankTypeProperty =
            DependencyProperty.Register(nameof(TankType), typeof(ModelsTankType), typeof(TankControl),
                new PropertyMetadata(ModelsTankType.CarbonBlack, OnTankTypeChanged));

        public static readonly DependencyProperty FillRatioProperty =
            DependencyProperty.Register(nameof(FillRatio), typeof(double), typeof(TankControl),
                new PropertyMetadata(0.0, OnFillRatioChanged, CoerceFillRatio));

        public static readonly DependencyProperty TemperatureProperty =
            DependencyProperty.Register(nameof(Temperature), typeof(double), typeof(TankControl),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty HighLevelStateProperty =
            DependencyProperty.Register(nameof(HighLevelState), typeof(bool), typeof(TankControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty LowLevelStateProperty =
            DependencyProperty.Register(nameof(LowLevelState), typeof(bool), typeof(TankControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty InletValveOpenProperty =
            DependencyProperty.Register(nameof(InletValveOpen), typeof(bool), typeof(TankControl),
                new PropertyMetadata(false, OnInletValveChanged));

        public static readonly DependencyProperty OutletValveOpenProperty =
            DependencyProperty.Register(nameof(OutletValveOpen), typeof(bool), typeof(TankControl),
                new PropertyMetadata(false, OnOutletValveChanged));

        public static readonly DependencyProperty ConeHeightRatioProperty =
            DependencyProperty.Register(nameof(ConeHeightRatio), typeof(double), typeof(TankControl),
                new PropertyMetadata(0.3, OnConeHeightRatioChanged));

        public string TankName
        {
            get => (string)GetValue(TankNameProperty);
            set => SetValue(TankNameProperty, value);
        }

        public ModelsTankType TankType
        {
            get => (ModelsTankType)GetValue(TankTypeProperty);
            set => SetValue(TankTypeProperty, value);
        }

        public double FillRatio
        {
            get => (double)GetValue(FillRatioProperty);
            set => SetValue(FillRatioProperty, value);
        }

        public double Temperature
        {
            get => (double)GetValue(TemperatureProperty);
            set => SetValue(TemperatureProperty, value);
        }

        public bool HighLevelState
        {
            get => (bool)GetValue(HighLevelStateProperty);
            set => SetValue(HighLevelStateProperty, value);
        }

        public bool LowLevelState
        {
            get => (bool)GetValue(LowLevelStateProperty);
            set => SetValue(LowLevelStateProperty, value);
        }

        public bool InletValveOpen
        {
            get => (bool)GetValue(InletValveOpenProperty);
            set => SetValue(InletValveOpenProperty, value);
        }

        public bool OutletValveOpen
        {
            get => (bool)GetValue(OutletValveOpenProperty);
            set => SetValue(OutletValveOpenProperty, value);
        }

        public double ConeHeightRatio
        {
            get => (double)GetValue(ConeHeightRatioProperty);
            set => SetValue(ConeHeightRatioProperty, value);
        }

        public double ConeHeight => ActualHeight * ConeHeightRatio;
        public double CylinderHeight => ActualHeight * (1 - ConeHeightRatio);

        #endregion

        #region 几何属性

        public PathGeometry ConeGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(ActualWidth, 0), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.7, ConeHeight), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.3, ConeHeight), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry FillGeometry
        {
            get
            {
                var fillHeight = CylinderHeight * (FillRatio / 100.0);
                var geometry = new PathGeometry();
                var figure = new PathFigure
                {
                    StartPoint = new Point(ActualWidth * 0.05, CylinderHeight),
                    IsClosed = true
                };
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.95, CylinderHeight), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.95, CylinderHeight - fillHeight), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.05, CylinderHeight - fillHeight), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry WaveGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(0, 0) };
                var waveHeight = 4.0;
                var waveLength = 30.0;
                var fillHeight = CylinderHeight * (FillRatio / 100.0);
                var yBase = CylinderHeight - fillHeight;

                figure.StartPoint = new Point(0, yBase);
                for (double x = 0; x <= ActualWidth; x += waveLength / 4)
                {
                    var y = yBase + Math.Sin(x / waveLength * Math.PI * 2) * waveHeight;
                    figure.Segments.Add(new LineSegment(new Point(x, y), true));
                }
                figure.Segments.Add(new LineSegment(new Point(ActualWidth, CylinderHeight), true));
                figure.Segments.Add(new LineSegment(new Point(0, CylinderHeight), true));
                figure.IsClosed = true;
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry InletValveGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(20, 0), true));
                figure.Segments.Add(new LineSegment(new Point(20, -15), true));
                figure.Segments.Add(new LineSegment(new Point(10, -15), true));
                figure.Segments.Add(new LineSegment(new Point(10, -25), true));
                figure.Segments.Add(new LineSegment(new Point(0, -15), true));
                figure.Segments.Add(new LineSegment(new Point(0, -15), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry OutletValveGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(20, 0), true));
                figure.Segments.Add(new LineSegment(new Point(20, 15), true));
                figure.Segments.Add(new LineSegment(new Point(10, 15), true));
                figure.Segments.Add(new LineSegment(new Point(10, 25), true));
                figure.Segments.Add(new LineSegment(new Point(0, 15), true));
                figure.Segments.Add(new LineSegment(new Point(0, 15), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry DustOutletGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(18, 0), true));
                figure.Segments.Add(new LineSegment(new Point(18, -20), true));
                figure.Segments.Add(new LineSegment(new Point(0, -20), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry InletValveGeometryWithRotation
        {
            get
            {
                var geometry = InletValveGeometry.Clone();
                var transform = new RotateTransform(InletValveOpen ? -45 : 0, 10, 10);
                geometry.Transform = transform;
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry OutletValveGeometryWithRotation
        {
            get
            {
                var geometry = OutletValveGeometry.Clone();
                var transform = new RotateTransform(OutletValveOpen ? 45 : 0, 10, 10);
                geometry.Transform = transform;
                geometry.Freeze();
                return geometry;
            }
        }

        #endregion

        public TankControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateTankStyle();
            UpdateFillLevel();
            UpdateValves();
            UpdateSensors();
            UpdateGeometry();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGeometry();
            UpdateFillLevel();
            UpdateClipRegion();
        }

        private static void OnTankNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.TankLabel.Text = control.TankName;
        }

        private static void OnTankTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.UpdateTankStyle();
        }

        private static void OnFillRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.UpdateFillLevel();
        }

        private static object CoerceFillRatio(DependencyObject d, object value)
        {
            var ratio = (double)value;
            return Math.Clamp(ratio, 0, 100);
        }

        private static void OnInletValveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.UpdateValves();
        }

        private static void OnOutletValveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.UpdateValves();
        }

        private static void OnConeHeightRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TankControl control)
                control.UpdateGeometry();
        }

        private void UpdateTankStyle()
        {
            switch (TankType)
            {
                case ModelsTankType.CarbonBlack:
                    CylinderBorder.Background = (Brush)Resources["TankCylinderBrush"];
                    ConePath.Fill = (Brush)Resources["TankConeBrush"];
                    CarbonFillPath.Visibility = Visibility.Visible;
                    OilFillPath.Visibility = Visibility.Collapsed;
                    TempDisplay.Visibility = Visibility.Collapsed;
                    break;
                case ModelsTankType.Oil:
                    CylinderBorder.Background = (Brush)Resources["OilTankCylinderBrush"];
                    ConePath.Fill = (Brush)Resources["OilTankConeBrush"];
                    CarbonFillPath.Visibility = Visibility.Collapsed;
                    OilFillPath.Visibility = Visibility.Visible;
                    TempDisplay.Visibility = Visibility.Visible;
                    break;
                case ModelsTankType.Rubber:
                    CarbonFillPath.Visibility = Visibility.Visible;
                    OilFillPath.Visibility = Visibility.Collapsed;
                    TempDisplay.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void UpdateFillLevel()
        {
            var fillHeight = CylinderHeight * (FillRatio / 100.0);

            // 更新填充几何体
            var fillGeometry = FillGeometry;
            CarbonFillPath.Data = fillGeometry;
            OilFillPath.Data = fillGeometry;

            // 更新裁剪区域
            FillClipRect.Rect = new Rect(0, CylinderHeight - fillHeight, ActualWidth, fillHeight);

            // 表面波纹
            var waveGeometry = WaveGeometry;
        }

        private void UpdateValves()
        {
            if (InletValvePath != null)
            {
                var transform = new RotateTransform(InletValveOpen ? -45 : 0, 10, 10);
                InletValvePath.RenderTransform = transform;
            }

            if (OutletValvePath != null)
            {
                var transform = new RotateTransform(OutletValveOpen ? 45 : 0, 10, 10);
                OutletValvePath.RenderTransform = transform;
            }
        }

        private void UpdateSensors()
        {
            if (HighLevelSensor != null)
            {
                HighLevelSensor.Fill = HighLevelState ? Brushes.LimeGreen : Brushes.Gray;
            }
            if (LowLevelSensor != null)
            {
                LowLevelSensor.Fill = LowLevelState ? Brushes.LimeGreen : Brushes.Gray;
            }
        }

        private void UpdateGeometry()
        {
            // 更新裁剪矩形
            if (FillClipRect != null)
            {
                var fillHeight = CylinderHeight * (FillRatio / 100.0);
                FillClipRect.Rect = new Rect(0, CylinderHeight - fillHeight, ActualWidth, fillHeight);
            }
        }

        private void UpdateClipRegion()
        {
            if (FillClipRect != null)
            {
                var fillHeight = CylinderHeight * (FillRatio / 100.0);
                FillClipRect.Rect = new Rect(0, CylinderHeight - fillHeight, ActualWidth, fillHeight);
            }
        }

        public override void UpdateVisualState()
        {
            base.UpdateVisualState();

            if (StatusLight != null && EquipmentModel != null)
            {
                StatusLight.Fill = EquipmentModel.State switch
                {
                    EquipmentState.Stopped => (Brush)Resources["StateStoppedBrush"],
                    EquipmentState.Running => (Brush)Resources["StateRunningBrush"],
                    EquipmentState.Warning => (Brush)Resources["StateWarningBrush"],
                    EquipmentState.Alarm => (Brush)Resources["StateAlarmBrush"],
                    EquipmentState.Maintenance => (Brush)Resources["StateMaintenanceBrush"],
                    _ => Brushes.Gray
                };
            }

            UpdateTankStyle();
            UpdateFillLevel();
            UpdateValves();
            UpdateSensors();
        }

        protected override void UpdateAnchors()
        {
            if (EquipmentModel == null) return;

            var anchors = EquipmentModel.Anchors;
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;
            var radius = Math.Min(ActualWidth, ActualHeight) / 2;

            foreach (var anchor in anchors)
            {
                Point pos = anchor.Type switch
                {
                    AnchorType.Top => new Point(centerX, 0),
                    AnchorType.Bottom => new Point(centerX, ActualHeight),
                    AnchorType.Left => new Point(0, centerY),
                    AnchorType.Right => new Point(ActualWidth, centerY),
                    AnchorType.TopLeft => new Point(0, 0),
                    AnchorType.TopRight => new Point(ActualWidth, 0),
                    AnchorType.BottomLeft => new Point(0, ActualHeight),
                    AnchorType.BottomRight => new Point(ActualWidth, ActualHeight),
                    _ => new Point(centerX, centerY)
                };

                anchor.Position = pos;
            }
        }

private Canvas _anchorsCanvas;

        protected override void UpdateAnchorsVisibility()
        {
            var visibility = (ShowAnchors || IsSelected || IsEditing) ? Visibility.Visible : Visibility.Collapsed;
            if (AnchorsControl != null) AnchorsControl.Visibility = visibility;

            // 连接点位置更新
            _anchorsCanvas ??= AnchorsControl.FindName("AnchorsCanvas") as Canvas;
            if (_anchorsCanvas != null)
            {
                foreach (var item in _anchorsCanvas.Children)
                {
                    if (item is FrameworkElement fe && fe.Tag is AnchorPoint anchor)
                    {
                        Canvas.SetLeft(fe, anchor.Position.X - 7);
                        Canvas.SetTop(fe, anchor.Position.Y - 7);
                    }
                }
            }
        }

        public override Rect GetBounds()
        {
            return new Rect(0, 0, ActualWidth, ActualHeight);
        }
    }
}


