using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class SideBlowDeviceControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty ValveStateProperty =
            DependencyProperty.Register(nameof(ValveState), typeof(ValveState), typeof(SideBlowDeviceControl),
                new PropertyMetadata(ValveState.Closed, OnValveStateChanged));

        public static readonly DependencyProperty BranchAngleProperty =
            DependencyProperty.Register(nameof(BranchAngle), typeof(double), typeof(SideBlowDeviceControl),
                new PropertyMetadata(45.0, OnBranchAngleChanged));

        public ValveState ValveState
        {
            get => (ValveState)GetValue(ValveStateProperty);
            set => SetValue(ValveStateProperty, value);
        }

        public double BranchAngle
        {
            get => (double)GetValue(BranchAngleProperty);
            set => SetValue(BranchAngleProperty, value);
        }

        public SideBlowDeviceControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateValveState();
            UpdateBranchAngle();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnValveStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideBlowDeviceControl control)
                control.UpdateValveState();
        }

        private static void OnBranchAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideBlowDeviceControl control)
                control.UpdateBranchAngle();
        }

        private void UpdateValveState()
        {
            if (ValveCore == null) return;

            var animation = new DoubleAnimation
            {
                To = ValveState switch
                {
                    ValveState.Closed => 0,
                    ValveState.Opening => -15,
                    ValveState.Open => -30,
                    ValveState.Closing => -15,
                    _ => 0
                },
                Duration = TimeSpan.FromSeconds(ValveState == ValveState.Opening || ValveState == ValveState.Closing ? 1.0 : 0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ValveCoreTranslate.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void UpdateBranchAngle()
        {
            if (BranchRotation == null) return;

            var animation = new DoubleAnimation
            {
                To = BranchAngle,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            BranchRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
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
            base.UpdateAnchors();
            if (EquipmentModel == null) return;

            double w = ActualWidth > 0 ? ActualWidth : Width;
            double h = ActualHeight > 0 ? ActualHeight : Height;

            foreach (var anchor in EquipmentModel.Anchors)
            {
                anchor.Position = anchor.Type switch
                {
                    AnchorType.Left => new Point(0, h * anchor.RelativePosition.Y),
                    AnchorType.Right => new Point(w, h * anchor.RelativePosition.Y),
                    AnchorType.Top => new Point(w * anchor.RelativePosition.X, 0),
                    AnchorType.Bottom => new Point(w * anchor.RelativePosition.X, h),
                    _ => new Point(w * anchor.RelativePosition.X, h * anchor.RelativePosition.Y)
                };
            }
        }

        public override Rect GetBounds()
        {
            return new Rect(EquipmentModel?.Position.X - Width / 2 ?? 0, EquipmentModel?.Position.Y - Height / 2 ?? 0, Width, Height);
        }
    }
}