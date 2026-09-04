using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;
using ModelsScaleType = TwoD5DMonitoring.Models.ScaleType;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class ScaleControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty ScaleTypeProperty =
            DependencyProperty.Register(nameof(ScaleType), typeof(ModelsScaleType), typeof(ScaleControl),
                new PropertyMetadata(ModelsScaleType.CarbonBlack, OnScaleTypeChanged));

        public static readonly DependencyProperty WeightProperty =
            DependencyProperty.Register(nameof(Weight), typeof(double), typeof(ScaleControl),
                new PropertyMetadata(0.0, OnWeightChanged));

        public static readonly DependencyProperty FillRatioProperty =
            DependencyProperty.Register(nameof(FillRatio), typeof(double), typeof(ScaleControl),
                new PropertyMetadata(0.0, OnFillRatioChanged, CoerceFillRatio));

        public static readonly DependencyProperty ShowSensorBracketProperty =
            DependencyProperty.Register(nameof(ShowSensorBracket), typeof(bool), typeof(ScaleControl),
                new PropertyMetadata(false));

        public ModelsScaleType ScaleType
        {
            get => (ModelsScaleType)GetValue(ScaleTypeProperty);
            set => SetValue(ScaleTypeProperty, value);
        }

        public double Weight
        {
            get => (double)GetValue(WeightProperty);
            set => SetValue(WeightProperty, value);
        }

        public double FillRatio
        {
            get => (double)GetValue(FillRatioProperty);
            set => SetValue(FillRatioProperty, value);
        }

        public bool ShowSensorBracket
        {
            get => (bool)GetValue(ShowSensorBracketProperty);
            set => SetValue(ShowSensorBracketProperty, value);
        }

        public ScaleControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateScaleStyle();
            UpdateFillLevel();
            UpdateSensorBracket();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnScaleTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScaleControl control)
                control.UpdateScaleStyle();
        }

private static void OnWeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScaleControl control && control.WeightDisplay != null)
                control.WeightDisplay.Text = $"{((double)e.NewValue):F1} kg";
        }

        private static void OnFillRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScaleControl control)
                control.UpdateFillLevel();
        }

        private static object CoerceFillRatio(DependencyObject d, object value)
        {
            var ratio = (double)value;
            return Math.Clamp(ratio, 0, 100);
        }

private void UpdateScaleStyle()
        {
            switch (ScaleType)
            {
                case ModelsScaleType.CarbonBlack:
                    if (FillPath != null && Resources["CarbonFillBrush"] is Brush carbonBrush)
                        FillPath.Fill = carbonBrush;
                    if (SensorBracket != null) SensorBracket.Visibility = Visibility.Visible;
                    break;
                case ModelsScaleType.Oil:
                    if (FillPath != null && Resources["OilFillBrush"] is Brush oilBrush)
                        FillPath.Fill = oilBrush;
                    if (SensorBracket != null) SensorBracket.Visibility = Visibility.Collapsed;
                    break;
                case ModelsScaleType.Rubber:
                    if (FillPath != null && Resources["RubberFillBrush"] is Brush rubberBrush)
                        FillPath.Fill = rubberBrush;
                    if (SensorBracket != null) SensorBracket.Visibility = Visibility.Visible;
                    break;
            }
        }

private void UpdateFillLevel()
        {
            if (FillPath == null) return;
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(20, 100), IsClosed = true };
            var fillHeight = 100 * (FillRatio / 100.0);
            figure.Segments.Add(new LineSegment(new Point(80, 100), true));
            figure.Segments.Add(new LineSegment(new Point(60, 100 - fillHeight), true));
            figure.Segments.Add(new LineSegment(new Point(40, 100 - fillHeight), true));
            figure.IsClosed = true;

            var geometry2 = new PathGeometry();
            geometry2.Figures.Add(figure);
            geometry2.Freeze();
            FillPath.Data = geometry2;
        }

private void UpdateSensorBracket()
        {
            // SensorBracket element not present in current XAML
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

            UpdateScaleStyle();
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

private Canvas _anchorsCanvas;

        protected override void UpdateAnchorsVisibility()
        {
            var visibility = (ShowAnchors || IsSelected || IsEditing) ? Visibility.Visible : Visibility.Collapsed;
            if (AnchorsControl != null) AnchorsControl.Visibility = visibility;

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
            return new Rect(0, 0, 100, 140);
        }
    }
}


