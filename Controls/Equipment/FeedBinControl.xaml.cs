using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class FeedBinControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty DoorStateProperty =
            DependencyProperty.Register(nameof(DoorState), typeof(DoorState), typeof(FeedBinControl),
                new PropertyMetadata(DoorState.Closed, OnDoorStateChanged));

        public static readonly DependencyProperty FillRatioProperty =
            DependencyProperty.Register(nameof(FillRatio), typeof(double), typeof(FeedBinControl),
                new PropertyMetadata(0.0, OnFillRatioChanged, CoerceFillRatio));

        public static readonly DependencyProperty DustLevelProperty =
            DependencyProperty.Register(nameof(DustLevel), typeof(double), typeof(FeedBinControl),
                new PropertyMetadata(0.0));

        public DoorState DoorState
        {
            get => (DoorState)GetValue(DoorStateProperty);
            set => SetValue(DoorStateProperty, value);
        }

        public double FillRatio
        {
            get => (double)GetValue(FillRatioProperty);
            set => SetValue(FillRatioProperty, value);
        }

        public double DustLevel
        {
            get => (double)GetValue(DustLevelProperty);
            set => SetValue(DustLevelProperty, value);
        }

        public FeedBinControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDoorAnimation();
            UpdateFillLevel();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnDoorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FeedBinControl control)
                control.UpdateDoorAnimation();
        }

        private static void OnFillRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FeedBinControl control)
                control.UpdateFillLevel();
        }

        private static object CoerceFillRatio(DependencyObject d, object value)
        {
            var ratio = (double)value;
            return Math.Clamp(ratio, 0, 100);
        }

private void UpdateDoorAnimation()
        {
            if (DoorTranslate == null) return;
            var animation = new DoubleAnimation
            {
                To = DoorState switch
                {
                    DoorState.Closed => 0,
                    DoorState.Opening => -30,
                    DoorState.Open => -40,
                    DoorState.Closing => -15,
                    _ => 0
                },
                Duration = TimeSpan.FromSeconds(DoorState == DoorState.Opening || DoorState == DoorState.Closing ? 1.5 : 0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            DoorTranslate.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        private void UpdateFillLevel()
        {
            if (FillPath != null)
            {
                var figure = new PathFigure { StartPoint = new Point(10, 220), IsClosed = true };
                var fillHeight = 200 * (FillRatio / 100.0);
                figure.Segments.Add(new LineSegment(new Point(130, 220), true));
                figure.Segments.Add(new LineSegment(new Point(120, 220 - fillHeight), true));
                figure.Segments.Add(new LineSegment(new Point(20, 220 - fillHeight), true));
                figure.IsClosed = true;

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                geometry.Freeze();
                FillPath.Data = geometry;
            }
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
                    AnchorType.Top => new Point(ActualWidth * 0.2, 0),
                    AnchorType.Bottom => new Point(ActualWidth / 2, ActualHeight),
                    AnchorType.Left => new Point(0, ActualHeight * 0.5),
                    AnchorType.Right => new Point(ActualWidth, ActualHeight * 0.5),
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
            return new Rect(0, 0, 140, 220);
        }
    }
}

