using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class PressurizedTankControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty FillRatioProperty =
            DependencyProperty.Register(nameof(FillRatio), typeof(double), typeof(PressurizedTankControl),
                new PropertyMetadata(0.0, OnFillRatioChanged, CoerceFillRatio));

        public static readonly DependencyProperty InletValveOpenProperty =
            DependencyProperty.Register(nameof(InletValveOpen), typeof(bool), typeof(PressurizedTankControl),
                new PropertyMetadata(false, OnInletValveChanged));

        public static readonly DependencyProperty OutletValveOpenProperty =
            DependencyProperty.Register(nameof(OutletValveOpen), typeof(bool), typeof(PressurizedTankControl),
                new PropertyMetadata(false, OnOutletValveChanged));

        public static readonly DependencyProperty HighLevelStateProperty =
            DependencyProperty.Register(nameof(HighLevelState), typeof(bool), typeof(PressurizedTankControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty LowLevelStateProperty =
            DependencyProperty.Register(nameof(LowLevelState), typeof(bool), typeof(PressurizedTankControl),
                new PropertyMetadata(false));

        public double FillRatio
        {
            get => (double)GetValue(FillRatioProperty);
            set => SetValue(FillRatioProperty, value);
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

        public double ConeHeight => ActualHeight * 0.4;
        public double CylinderHeight => ActualHeight * 0.6;

        public PathGeometry ConeGeometry
        {
            get
            {
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(15, 0), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(ActualWidth - 15, 0), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth - 15, CylinderHeight), true));
                figure.Segments.Add(new LineSegment(new Point(15, CylinderHeight), true));
                geometry.Figures.Add(figure);
                geometry.Freeze();
                return geometry;
            }
        }

        public PathGeometry FillGeometry
        {
            get
            {
                var fillHeight = (CylinderHeight + ConeHeight) * (FillRatio / 100.0);
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(10, CylinderHeight + ConeHeight), IsClosed = true };
                figure.Segments.Add(new LineSegment(new Point(ActualWidth - 10, CylinderHeight + ConeHeight), true));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth - 10, CylinderHeight + ConeHeight - fillHeight), true));
                figure.Segments.Add(new LineSegment(new Point(10, CylinderHeight + ConeHeight - fillHeight), true));
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

        public PressurizedTankControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateFillLevel();
            UpdateValves();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnFillRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PressurizedTankControl control)
                control.UpdateFillLevel();
        }

        private static object CoerceFillRatio(DependencyObject d, object value)
        {
            var ratio = (double)value;
            return Math.Clamp(ratio, 0, 100);
        }

        private static void OnInletValveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PressurizedTankControl control)
                control.UpdateValves();
        }

        private static void OnOutletValveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PressurizedTankControl control)
                control.UpdateValves();
        }

        private void UpdateFillLevel()
        {
            // 更新料位几何
        }

        private void UpdateValves()
        {
            // 更新阀门动画
        }

        public override void UpdateVisualState()
        {
            base.UpdateVisualState();

            if (StatusLight != null)
            {
                StatusLight.Fill = EquipmentModel?.State switch
                {
                    EquipmentState.Stopped => Brushes.Gray,
                    EquipmentState.Running => Brushes.LimeGreen,
                    EquipmentState.Warning => Brushes.Gold,
                    EquipmentState.Alarm => Brushes.Red,
                    EquipmentState.Maintenance => Brushes.Blue,
                    _ => Brushes.Gray
                };
            }
        }

        protected override void UpdateAnchors()
        {
            if (EquipmentModel == null) return;

            foreach (var anchor in EquipmentModel.Anchors)
            {
                Point pos = anchor.Type switch
                {
                    AnchorType.Top => new Point(ActualWidth / 2, 0),
                    AnchorType.Bottom => new Point(ActualWidth / 2, ActualHeight),
                    AnchorType.Left => new Point(0, ActualHeight / 2),
                    AnchorType.Right => new Point(ActualWidth, ActualHeight / 2),
                    AnchorType.TopLeft => new Point(0, 0),
                    AnchorType.TopRight => new Point(ActualWidth, 0),
                    AnchorType.BottomLeft => new Point(0, ActualHeight),
                    AnchorType.BottomRight => new Point(ActualWidth, ActualHeight),
                    _ => new Point(ActualWidth / 2, ActualHeight / 2)
                };
                anchor.Position = pos;
            }
        }

        protected override void UpdateAnchorsVisibility()
        {
            var visibility = (ShowAnchors || IsSelected || IsEditing) ? Visibility.Visible : Visibility.Collapsed;
            if (AnchorsControl != null) AnchorsControl.Visibility = visibility;
        }

        public override Rect GetBounds()
        {
            return new Rect(0, 0, ActualWidth, ActualHeight);
        }
    }
}

