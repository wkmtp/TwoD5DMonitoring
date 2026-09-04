using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using TwoD5DMonitoring.Models;
using ModelsFlowDirection = TwoD5DMonitoring.Models.FlowDirection;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class OilPumpControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty RotorSpeedProperty =
            DependencyProperty.Register(nameof(RotorSpeed), typeof(double), typeof(OilPumpControl),
                new PropertyMetadata(0.0, OnRotorSpeedChanged));

        public static readonly DependencyProperty MotorStateProperty =
            DependencyProperty.Register(nameof(MotorState), typeof(MotorState), typeof(OilPumpControl),
                new PropertyMetadata(MotorState.Stopped, OnMotorStateChanged));

        public static readonly DependencyProperty FlowDirectionProperty =
            DependencyProperty.Register(nameof(FlowDirection), typeof(ModelsFlowDirection), typeof(OilPumpControl),
                new PropertyMetadata(ModelsFlowDirection.Forward));

        public double RotorSpeed
        {
            get => (double)GetValue(RotorSpeedProperty);
            set => SetValue(RotorSpeedProperty, value);
        }

        public MotorState MotorState
        {
            get => (MotorState)GetValue(MotorStateProperty);
            set => SetValue(MotorStateProperty, value);
        }

        public ModelsFlowDirection FlowDirection
        {
            get => (ModelsFlowDirection)GetValue(FlowDirectionProperty);
            set => SetValue(FlowDirectionProperty, value);
        }

        private Storyboard _rotorStoryboard;

        public OilPumpControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var storyboard = FindResource("RotorAnimation") as Storyboard;
            if (storyboard != null)
            {
                _rotorStoryboard = storyboard;
                if (MotorState == MotorState.Running)
                    storyboard.Begin(this, true);
            }
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnRotorSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OilPumpControl control)
            {
                // 根据速度调整动画时长
            }
        }

        private static void OnMotorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OilPumpControl control)
            {
                var state = (MotorState)e.NewValue;
                var storyboard = control.FindResource("RotorAnimation") as Storyboard;
                if (storyboard != null)
                {
                    switch (state)
                    {
                        case MotorState.Running:
                            storyboard.Begin(control, true);
                            break;
                        case MotorState.Stopped:
                        case MotorState.Fault:
                            storyboard.Stop(control);
                            break;
                    }
                }
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
            return new Rect(0, 0, 90, 90);
        }
    }
}

