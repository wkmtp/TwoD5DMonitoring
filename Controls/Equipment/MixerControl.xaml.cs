using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class MixerControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty FeedDoorStateProperty =
            DependencyProperty.Register(nameof(FeedDoorState), typeof(DoorState), typeof(MixerControl),
                new PropertyMetadata(DoorState.Closed, OnFeedDoorStateChanged));

        public static readonly DependencyProperty DischargeDoorStateProperty =
            DependencyProperty.Register(nameof(DischargeDoorState), typeof(DoorState), typeof(MixerControl),
                new PropertyMetadata(DoorState.Closed, OnDischargeDoorStateChanged));

        public static readonly DependencyProperty RotorAnimationSpeedProperty =
            DependencyProperty.Register(nameof(RotorAnimationSpeed), typeof(double), typeof(MixerControl),
                new PropertyMetadata(1.0, OnRotorAnimationSpeedChanged));

        public DoorState FeedDoorState
        {
            get => (DoorState)GetValue(FeedDoorStateProperty);
            set => SetValue(FeedDoorStateProperty, value);
        }

        public DoorState DischargeDoorState
        {
            get => (DoorState)GetValue(DischargeDoorStateProperty);
            set => SetValue(DischargeDoorStateProperty, value);
        }

        public double RotorAnimationSpeed
        {
            get => (double)GetValue(RotorAnimationSpeedProperty);
            set => SetValue(RotorAnimationSpeedProperty, value);
        }

        public MixerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDoorAnimations();
            UpdateRotorSpeed();
            UpdateAnchors();
            UpdateAnchorsVisibility();

            // Start rotor animations
            if (Resources["Rotor1Animation"] is Storyboard rotor1Anim)
                rotor1Anim.Begin(this, true);
            if (Resources["Rotor2Animation"] is Storyboard rotor2Anim)
                rotor2Anim.Begin(this, true);

            if (EquipmentModel is MixerModel model)
            {
                model.FeedDoorState = FeedDoorState;
                model.DischargeDoorState = DischargeDoorState;
                model.RotorAnimationSpeed = RotorAnimationSpeed;
            }
        }

        private static void OnFeedDoorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MixerControl control)
                control.UpdateDoorAnimations();
        }

        private static void OnDischargeDoorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MixerControl control)
                control.UpdateDoorAnimations();
        }

        private static void OnRotorAnimationSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MixerControl control)
                control.UpdateRotorSpeed();
        }

        private void UpdateDoorAnimations()
        {
            if (FeedDoorState == DoorState.Opening || FeedDoorState == DoorState.Open)
            {
                if (Resources["FeedDoorOpenAnimation"] is Storyboard anim)
                    anim.Begin(this, true);
            }
            else if (FeedDoorState == DoorState.Closing || FeedDoorState == DoorState.Closed)
            {
                if (Resources["FeedDoorCloseAnimation"] is Storyboard anim)
                    anim.Begin(this, true);
            }

            if (DischargeDoorState == DoorState.Opening || DischargeDoorState == DoorState.Open)
            {
                if (Resources["DischargeDoorOpenAnimation"] is Storyboard anim)
                    anim.Begin(this, true);
            }
            else if (DischargeDoorState == DoorState.Closing || DischargeDoorState == DoorState.Closed)
            {
                if (Resources["DischargeDoorCloseAnimation"] is Storyboard anim)
                    anim.Begin(this, true);
            }
        }

        private void UpdateRotorSpeed()
        {
            double speed = Math.Max(0.1, RotorAnimationSpeed);
            double duration = 2.0 / speed;

            // Rotor 1
            var anim1 = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(duration),
                RepeatBehavior = RepeatBehavior.Forever
            };
            var sb1 = new Storyboard();
            sb1.Children.Add(anim1);
            Storyboard.SetTarget(anim1, Rotor1Rotation);
            Storyboard.SetTargetProperty(anim1, new PropertyPath(RotateTransform.AngleProperty));
            sb1.RepeatBehavior = RepeatBehavior.Forever;
            sb1.Begin(this, true);

            // Rotor 2 (counter-rotating)
            var anim2 = new DoubleAnimation
            {
                From = 180,
                To = 540,
                Duration = TimeSpan.FromSeconds(duration),
                RepeatBehavior = RepeatBehavior.Forever
            };
            var sb2 = new Storyboard();
            sb2.Children.Add(anim2);
            Storyboard.SetTarget(anim2, Rotor2Rotation);
            Storyboard.SetTargetProperty(anim2, new PropertyPath(RotateTransform.AngleProperty));
            sb2.RepeatBehavior = RepeatBehavior.Forever;
            sb2.Begin(this, true);
        }

        public void OpenFeedDoor()
        {
            FeedDoorState = DoorState.Opening;
        }

        public void CloseFeedDoor()
        {
            FeedDoorState = DoorState.Closing;
        }

        public void OpenDischargeDoor()
        {
            DischargeDoorState = DoorState.Opening;
        }

        public void CloseDischargeDoor()
        {
            DischargeDoorState = DoorState.Closing;
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

            // Update parameter panel
            if (EquipmentModel is MixerModel model)
            {
                if (TempValue != null)
                    TempValue.Text = $"{model.Temperature:F1} °C";
                if (PressureValue != null)
                    PressureValue.Text = $"{model.Pressure:F2} MPa";
                if (SpeedValue != null)
                    SpeedValue.Text = $"{model.Speed:F0} rpm";
                if (PowerValue != null)
                    PowerValue.Text = $"{model.Power:F1} kW";
                if (EnergyValue != null)
                    EnergyValue.Text = $"{model.Energy:F1} kWh";
            }

            // Update gauges
            if (TempNeedleRotation != null && EquipmentModel is MixerModel m)
            {
                double tempAngle = Math.Max(-90, Math.Min(90, -90 + m.Temperature * 0.45)); // 0-400°C
                TempNeedleRotation.Angle = tempAngle;
            }

            if (PressureNeedleRotation != null && EquipmentModel is MixerModel m2)
            {
                double pressureAngle = Math.Max(-90, Math.Min(90, -90 + m2.Pressure * 90)); // 0-2MPa
                PressureNeedleRotation.Angle = pressureAngle;
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
                    AnchorType.Top => new Point(w * anchor.RelativePosition.X, 0),
                    AnchorType.Bottom => new Point(w * anchor.RelativePosition.X, h),
                    AnchorType.Left => new Point(0, h * anchor.RelativePosition.Y),
                    AnchorType.Right => new Point(w, h * anchor.RelativePosition.Y),
                    AnchorType.TopLeft => new Point(0, 0),
                    AnchorType.TopRight => new Point(w, 0),
                    AnchorType.BottomLeft => new Point(0, h),
                    AnchorType.BottomRight => new Point(w, h),
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