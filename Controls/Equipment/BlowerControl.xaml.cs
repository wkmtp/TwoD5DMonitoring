using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class BlowerControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty BlowerStyleProperty =
            DependencyProperty.Register(nameof(BlowerStyle), typeof(BlowerStyle), typeof(BlowerControl),
                new PropertyMetadata(BlowerStyle.Standard, OnBlowerStyleChanged));

        public static readonly DependencyProperty RotorSpeedProperty =
            DependencyProperty.Register(nameof(RotorSpeed), typeof(double), typeof(BlowerControl),
                new PropertyMetadata(1500.0, OnRotorSpeedChanged));

        public static readonly DependencyProperty ImpellerCountProperty =
            DependencyProperty.Register(nameof(ImpellerCount), typeof(int), typeof(BlowerControl),
                new PropertyMetadata(2, OnImpellerCountChanged));

        public static readonly DependencyProperty MotorStateProperty =
            DependencyProperty.Register(nameof(MotorState), typeof(MotorState), typeof(BlowerControl),
                new PropertyMetadata(MotorState.Stopped, OnMotorStateChanged));

        public BlowerStyle BlowerStyle
        {
            get => (BlowerStyle)GetValue(BlowerStyleProperty);
            set => SetValue(BlowerStyleProperty, value);
        }

        public double RotorSpeed
        {
            get => (double)GetValue(RotorSpeedProperty);
            set => SetValue(RotorSpeedProperty, value);
        }

        public int ImpellerCount
        {
            get => (int)GetValue(ImpellerCountProperty);
            set => SetValue(ImpellerCountProperty, value);
        }

        public MotorState MotorState
        {
            get => (MotorState)GetValue(MotorStateProperty);
            set => SetValue(MotorStateProperty, value);
        }

        public BlowerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateBlowerStyle();
            UpdateImpellerCount();
            UpdateMotorState();
            UpdateAnchors();
            UpdateAnchorsVisibility();

            if (EquipmentModel is RootBlowerModel model)
            {
                model.MotorState = MotorState;
                model.RotorSpeed = RotorSpeed;
                model.ImpellerCount = ImpellerCount;
            }

            // Start animations
            if (Resources["ImpellerAnimation"] is Storyboard impellerAnim)
            {
                impellerAnim.Begin(this, true);
            }
            if (Resources["PulleyAnimation"] is Storyboard pulleyAnim)
            {
                pulleyAnim.Begin(this, true);
            }
        }

        private static void OnBlowerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlowerControl control)
                control.UpdateBlowerStyle();
        }

        private static void OnRotorSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlowerControl control)
                control.UpdateRotorSpeed();
        }

        private static void OnImpellerCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlowerControl control)
                control.UpdateImpellerCount();
        }

        private static void OnMotorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlowerControl control)
                control.UpdateMotorState();
        }

        private void UpdateBlowerStyle()
        {
            switch (BlowerStyle)
            {
                case BlowerStyle.HighPressure:
                    Width = 200;
                    Height = 140;
                    break;
                default:
                    Width = 180;
                    Height = 120;
                    break;
            }
        }

        private void UpdateRotorSpeed()
        {
            if (Resources["ImpellerAnimation"] is Storyboard anim)
            {
                anim.Stop(this);
                var duration = TimeSpan.FromSeconds(Math.Max(0.2, 60.0 / Math.Max(100, RotorSpeed)));
                var doubleAnim = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = duration,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(doubleAnim, Impeller1Rotation);
                Storyboard.SetTargetProperty(doubleAnim, new PropertyPath(RotateTransform.AngleProperty));
                Storyboard.SetTargetName(doubleAnim, "Impeller1Rotation");

                var anim2 = new Storyboard();
                anim2.Children.Add(doubleAnim);
                anim2.RepeatBehavior = RepeatBehavior.Forever;
                anim2.Begin(this, true);

                var doubleAnim2 = new DoubleAnimation
                {
                    From = 180,
                    To = 540,
                    Duration = duration,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                var anim3 = new Storyboard();
                anim3.Children.Add(doubleAnim2);
                anim3.RepeatBehavior = RepeatBehavior.Forever;
                anim3.Begin(this, true);

                // Update pulley speed too
                if (Resources["PulleyAnimation"] is Storyboard pulleyAnim)
                {
                    var pulleyDuration = TimeSpan.FromSeconds(Math.Max(0.4, 120.0 / Math.Max(100, RotorSpeed)));
                    var pulleyDoubleAnim = new DoubleAnimation
                    {
                        From = 0,
                        To = 360,
                        Duration = pulleyDuration,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    var pulleyStoryboard = new Storyboard();
                    pulleyStoryboard.Children.Add(pulleyDoubleAnim);
                    pulleyStoryboard.RepeatBehavior = RepeatBehavior.Forever;
                    pulleyStoryboard.Begin(this, true);
                }
            }
        }

        private void UpdateImpellerCount()
        {
            Impeller2.Visibility = ImpellerCount >= 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateMotorState()
        {
            if (EquipmentModel is RootBlowerModel model)
            {
                model.MotorState = MotorState;
            }

            bool isRunning = MotorState == MotorState.Running;
            if (Resources["ImpellerAnimation"] is Storyboard impellerAnim)
            {
                if (isRunning)
                    impellerAnim.Begin(this, true);
                else
                    impellerAnim.Stop(this);
            }
            if (Resources["PulleyAnimation"] is Storyboard pulleyAnim)
            {
                if (isRunning)
                    pulleyAnim.Begin(this, true);
                else
                    pulleyAnim.Stop(this);
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

            if (GaugeNeedle != null && EquipmentModel is RootBlowerModel model)
            {
                double pressure = model.RotorSpeed * 0.005; // Simulated pressure
                double angle = Math.Max(-90, Math.Min(90, -90 + pressure * 180));
                if (NeedleRotation != null)
                    NeedleRotation.Angle = angle;
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