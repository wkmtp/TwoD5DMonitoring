using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Pipes
{
    public partial class PipeControl : UserControl
    {
        public PipeControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            ControlPoints = new ObservableCollection<Point>();
            ControlPointsControl.ItemsSource = ControlPoints;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateVisualStyle();
            SetupFlowAnimation();
            UpdateControlPoints();
        }

        public PipeSegment PipeModel
        {
            get => (PipeSegment)GetValue(PipeModelProperty);
            set => SetValue(PipeModelProperty, value);
        }

        public static readonly DependencyProperty PipeModelProperty =
            DependencyProperty.Register(nameof(PipeModel), typeof(PipeSegment), typeof(PipeControl),
                new PropertyMetadata(null, OnPipeModelChanged));

        private static void OnPipeModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.OnPipeModelChanged(e.OldValue as PipeSegment, e.NewValue as PipeSegment);
            }
        }

        private void OnPipeModelChanged(PipeSegment oldModel, PipeSegment newModel)
        {
            if (oldModel != null)
            {
                oldModel.PropertyChanged -= OnPipeModelPropertyChanged;
            }
            if (newModel != null)
            {
                newModel.PropertyChanged += OnPipeModelPropertyChanged;
            }
            UpdateVisualStyle();
            UpdateGeometry();
            UpdateControlPoints();
            UpdateFlowAnimationSpeed();
        }

        private void OnPipeModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PipeSegment.StartPoint):
                case nameof(PipeSegment.EndPoint):
                case nameof(PipeSegment.ControlPoint1):
                case nameof(PipeSegment.ControlPoint2):
                case nameof(PipeSegment.PipeType):
                    Dispatcher.Invoke(() =>
                    {
                        UpdateGeometry();
                        UpdateControlPoints();
                    });
                    break;
                case nameof(PipeSegment.FlowSpeed):
                    Dispatcher.Invoke(UpdateFlowAnimationSpeed);
                    break;
                case nameof(PipeSegment.IsAnimating):
                    Dispatcher.Invoke(() =>
                    {
                        if (PipeModel.IsAnimating) StartFlowAnimation();
                        else StopFlowAnimation();
                    });
                    break;
            }
        }

        public double Diameter
        {
            get => (double)GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(PipeControl),
                new PropertyMetadata(20.0, OnDiameterChanged));

        private static void OnDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.UpdateVisualStyle();
            }
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(PipeControl),
                new PropertyMetadata(false, OnIsSelectedChanged));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.UpdateSelectionVisual();
            }
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(PipeControl),
                new PropertyMetadata(false, OnIsEditingChanged));

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.UpdateEditingVisual();
            }
        }

        public bool ShowAnchors
        {
            get => (bool)GetValue(ShowAnchorsProperty);
            set => SetValue(ShowAnchorsProperty, value);
        }

        public static readonly DependencyProperty ShowAnchorsProperty =
            DependencyProperty.Register(nameof(ShowAnchors), typeof(bool), typeof(PipeControl),
                new PropertyMetadata(false, OnShowAnchorsChanged));

        private static void OnShowAnchorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.UpdateAnchorsVisibility();
            }
        }

        public bool ShowControlPoints
        {
            get => (bool)GetValue(ShowControlPointsProperty);
            set => SetValue(ShowControlPointsProperty, value);
        }

        public static readonly DependencyProperty ShowControlPointsProperty =
            DependencyProperty.Register(nameof(ShowControlPoints), typeof(bool), typeof(PipeControl),
                new PropertyMetadata(false, OnShowControlPointsChanged));

        private static void OnShowControlPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeControl control)
            {
                control.UpdateControlPointsVisibility();
            }
        }

        public ObservableCollection<Point> ControlPoints { get; private set; }

        public bool IsAnimating
        {
            get => PipeModel?.IsAnimating ?? false;
            set
            {
                if (PipeModel != null)
                    PipeModel.IsAnimating = value;
            }
        }

        private Storyboard _flowStoryboard;
        private DoubleAnimation _dashOffsetAnimation;

        private void UpdateVisualStyle()
        {
            if (PipeModel == null) return;

            LinearGradientBrush brush;

            switch (PipeModel.PipeType)
            {
                case PipeType.CarbonConveying:
                    brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0x10, 0x10, 0x10), 0.0),
                            new GradientStop(Color.FromRgb(0x1A, 0x1A, 0x1A), 0.3),
                            new GradientStop(Color.FromRgb(0x15, 0x15, 0x15), 0.7),
                            new GradientStop(Color.FromRgb(0x0A, 0x0A, 0x0A), 1.0)
                        }
                    };
                    break;
                case PipeType.OilSupply:
                    brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0xFF, 0xD7, 0x00), 0.0),
                            new GradientStop(Color.FromRgb(0xDA, 0xA5, 0x20), 0.4),
                            new GradientStop(Color.FromRgb(0xB8, 0x86, 0x0B), 0.8),
                            new GradientStop(Color.FromRgb(0x8B, 0x69, 0x14), 1.0)
                        }
                    };
                    break;
                case PipeType.CoolingWater:
                    brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0x00, 0x66, 0xCC), 0.0),
                            new GradientStop(Color.FromRgb(0x00, 0x44, 0x99), 0.5),
                            new GradientStop(Color.FromRgb(0x00, 0x22, 0x66), 1.0)
                        }
                    };
                    break;
                case PipeType.CompressedAir:
                    brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0x88, 0xAA, 0xCC), 0.0),
                            new GradientStop(Color.FromRgb(0x66, 0x88, 0xAA), 0.5),
                            new GradientStop(Color.FromRgb(0x44, 0x66, 0x88), 1.0)
                        }
                    };
                    break;
                case PipeType.DustCollection:
                    brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0x55, 0x55, 0x55), 0.0),
                            new GradientStop(Color.FromRgb(0x33, 0x33, 0x33), 0.5),
                            new GradientStop(Color.FromRgb(0x11, 0x11, 0x11), 1.0)
                        }
                    };
                    break;
                default:
                    brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
                    brush.GradientStops.Add(new GradientStop(Colors.Gray, 0));
                    brush.GradientStops.Add(new GradientStop(Colors.Gray, 1));
                    break;
            }

            MainPipePath.Stroke = brush;
        }

        private void UpdateGeometry()
        {
            if (PipeModel == null) return;

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = PipeModel.StartPoint };

            if (PipeModel.ControlPoint1 != null && PipeModel.ControlPoint2 != null)
            {
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = PipeModel.ControlPoint1.Value,
                    Point2 = PipeModel.ControlPoint2.Value,
                    Point3 = PipeModel.EndPoint
                });
            }
            else
            {
                figure.Segments.Add(new LineSegment { Point = PipeModel.EndPoint });
            }

            geometry.Figures.Add(figure);
            PipeGeometry.Figures.Clear();
            PipeGeometry.Figures.Add(figure);

            ShadowGeometry.Figures.Clear();
            ShadowGeometry.Figures.Add(figure);

            HighlightGeometry.Figures.Clear();
            HighlightGeometry.Figures.Add(figure);

            ParticleGeometry.Figures.Clear();
            ParticleGeometry.Figures.Add(figure);

            UpdateArrowPosition();
        }

        private Point GetPointAtFractionLength(PathGeometry geometry, double fraction, out Point tangent)
        {
            double totalLength = 0;
            foreach (var fig in geometry.Figures)
            {
                foreach (var seg in fig.Segments)
                {
                    if (seg is LineSegment ls)
                    {
                        double segLen = Math.Sqrt(Math.Pow(ls.Point.X - fig.StartPoint.X, 2) + Math.Pow(ls.Point.Y - fig.StartPoint.Y, 2));
                        totalLength += segLen;
                    }
                    else if (seg is BezierSegment bs)
                    {
                        // Approximate bezier length
                        double segLen = Math.Sqrt(Math.Pow(bs.Point3.X - fig.StartPoint.X, 2) + Math.Pow(bs.Point3.Y - fig.StartPoint.Y, 2));
                        totalLength += segLen;
                    }
                }
            }

            double targetLength = totalLength * fraction;
            double currentLength = 0;
            Point lastPoint = new Point();

            foreach (var fig in geometry.Figures)
            {
                lastPoint = fig.StartPoint;
                foreach (var seg in fig.Segments)
                {
                    double segLen = 0;
                    Point endPoint = new Point();

                    if (seg is LineSegment ls)
                    {
                        endPoint = ls.Point;
                        segLen = Math.Sqrt(Math.Pow(ls.Point.X - lastPoint.X, 2) + Math.Pow(ls.Point.Y - lastPoint.Y, 2));
                    }
                    else if (seg is BezierSegment bs)
                    {
                        endPoint = bs.Point3;
                        segLen = Math.Sqrt(Math.Pow(bs.Point3.X - lastPoint.X, 2) + Math.Pow(bs.Point3.Y - lastPoint.Y, 2));
                    }

                    if (currentLength + segLen >= targetLength)
                    {
                        double ratio = (targetLength - currentLength) / segLen;
                        double x = lastPoint.X + (endPoint.X - lastPoint.X) * ratio;
                        double y = lastPoint.Y + (endPoint.Y - lastPoint.Y) * ratio;
                        tangent = new Point(endPoint.X - lastPoint.X, endPoint.Y - lastPoint.Y);
                        return new Point(x, y);
                    }

                    currentLength += segLen;
                    lastPoint = endPoint;
                }
            }

            tangent = new Point(1, 0);
            return lastPoint;
        }

        private void UpdateArrowPosition()
        {
            if (PipeModel == null) return;

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = PipeModel.StartPoint };

            if (PipeModel.ControlPoint1 != null && PipeModel.ControlPoint2 != null)
            {
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = PipeModel.ControlPoint1.Value,
                    Point2 = PipeModel.ControlPoint2.Value,
                    Point3 = PipeModel.EndPoint
                });
            }
            else
            {
                figure.Segments.Add(new LineSegment { Point = PipeModel.EndPoint });
            }

            geometry.Figures.Add(figure);

            var pointAtHalf = GetPointAtFractionLength(geometry, 0.5, out Point tangent);
            var angle = Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI;

            if (ArrowRotation != null)
                ArrowRotation.Angle = angle;
            if (ArrowTranslate != null)
            {
                ArrowTranslate.X = pointAtHalf.X - 4;
                ArrowTranslate.Y = pointAtHalf.Y;
            }
        }

        private void UpdateControlPoints()
        {
            if (PipeModel == null) return;

            ControlPoints.Clear();
            if (PipeModel.ControlPoint1.HasValue)
                ControlPoints.Add(PipeModel.ControlPoint1.Value);
            if (PipeModel.ControlPoint2.HasValue)
                ControlPoints.Add(PipeModel.ControlPoint2.Value);
        }

        private void UpdateEditingVisual()
        {
            ShowControlPoints = IsEditing;
            UpdateControlPointsVisibility();
        }

        private void UpdateAnchorsVisibility()
        {
            var visibility = ShowAnchors ? Visibility.Visible : Visibility.Collapsed;
            StartAnchor.Visibility = visibility;
            EndAnchor.Visibility = visibility;
        }

        private void UpdateControlPointsVisibility()
        {
            var visibility = ShowControlPoints ? Visibility.Visible : Visibility.Collapsed;
            ControlPointsControl.Visibility = visibility;
        }

        private void SetupFlowAnimation()
        {
            _flowStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

            _dashOffsetAnimation = new DoubleAnimation
            {
                From = 0,
                To = 20,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            Storyboard.SetTarget(_dashOffsetAnimation, ParticlePath);
            Storyboard.SetTargetProperty(_dashOffsetAnimation, new PropertyPath(Path.StrokeDashOffsetProperty));

            _flowStoryboard.Children.Add(_dashOffsetAnimation);
        }

        public void UpdateFlowAnimationSpeed()
        {
            if (_dashOffsetAnimation != null && PipeModel != null)
            {
                var duration = TimeSpan.FromSeconds(Math.Max(0.5, 2.0 / PipeModel.FlowSpeed));
                _dashOffsetAnimation.Duration = new Duration(duration);
            }
        }

        private void UpdateSelectionVisual()
        {
            if (IsSelected)
            {
                MainPipePath.StrokeThickness = Diameter + 4;
                MainPipePath.Stroke = Brushes.Orange;
                MainPipePath.Effect = new DropShadowEffect
                {
                    Color = Colors.Orange,
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
            }
            else
            {
                MainPipePath.StrokeThickness = Diameter;
                UpdateVisualStyle();
                MainPipePath.Effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 2,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
            }
        }

        private void StartFlowAnimation()
        {
            if (_flowStoryboard != null && !IsAnimating)
            {
                _flowStoryboard.Begin(this, true);
            }
        }

        private void StopFlowAnimation()
        {
            _flowStoryboard?.Stop(this);
        }

        public void StartAnimation()
        {
            if (PipeModel != null)
            {
                PipeModel.IsAnimating = true;
            }
        }

        public void StopAnimation()
        {
            if (PipeModel != null)
            {
                PipeModel.IsAnimating = false;
            }
        }
    }
}