using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class SideBlowValveControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty ValveAngleProperty =
            DependencyProperty.Register(nameof(ValveAngle), typeof(double), typeof(SideBlowValveControl),
                new PropertyMetadata(0.0, OnValveAngleChanged));

        public static readonly DependencyProperty ValveStateProperty =
            DependencyProperty.Register(nameof(ValveState), typeof(ValveState), typeof(SideBlowValveControl),
                new PropertyMetadata(ValveState.Closed, OnValveStateChanged));

        public static readonly DependencyProperty BlowAngleProperty =
            DependencyProperty.Register(nameof(BlowAngle), typeof(double), typeof(SideBlowValveControl),
                new PropertyMetadata(45.0));

        public double ValveAngle
        {
            get => (double)GetValue(ValveAngleProperty);
            set => SetValue(ValveAngleProperty, value);
        }

        public ValveState ValveState
        {
            get => (ValveState)GetValue(ValveStateProperty);
            set => SetValue(ValveStateProperty, value);
        }

        public double BlowAngle
        {
            get => (double)GetValue(BlowAngleProperty);
            set => SetValue(BlowAngleProperty, value);
        }

        public SideBlowValveControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateValvePosition();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnValveAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideBlowValveControl control)
                control.UpdateValvePosition();
        }

        private static void OnValveStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideBlowValveControl control)
                control.UpdateValveAnimation();
        }

        private void UpdateValvePosition()
        {
            if (CoreTranslate != null)
            {
                var targetAngle = ValveState switch
                {
                    ValveState.Closed => 0,
                    ValveState.Opening => ValveAngle * 0.5,
                    ValveState.Open => ValveAngle,
                    ValveState.Closing => ValveAngle * 0.5,
                    _ => 0
                };

                var animation = new DoubleAnimation
                {
                    To = targetAngle,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                CoreTranslate.BeginAnimation(TranslateTransform.XProperty, animation);
            }
        }

        private void UpdateValveAnimation()
        {
            UpdateValvePosition();
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
                    AnchorType.Left => new Point(0, ActualHeight * 0.3),
                    AnchorType.Right => new Point(ActualWidth, ActualHeight * 0.3),
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
            return new Rect(0, 0, 60, 80);
        }
    }
}

