using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class DustCollectorControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty MotorStateProperty =
            DependencyProperty.Register(nameof(MotorState), typeof(MotorState), typeof(DustCollectorControl),
                new PropertyMetadata(MotorState.Stopped, OnMotorStateChanged));

        public static readonly DependencyProperty MotorSpeedProperty =
            DependencyProperty.Register(nameof(MotorSpeed), typeof(double), typeof(DustCollectorControl),
                new PropertyMetadata(0.0));

        public MotorState MotorState
        {
            get => (MotorState)GetValue(MotorStateProperty);
            set => SetValue(MotorStateProperty, value);
        }

        public double MotorSpeed
        {
            get => (double)GetValue(MotorSpeedProperty);
            set => SetValue(MotorSpeedProperty, value);
        }

        private Storyboard _motorStoryboard;
        private DoubleAnimation _finAnimation;

        public DustCollectorControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetupMotorAnimation();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnMotorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DustCollectorControl control)
            {
                var state = (MotorState)e.NewValue;
                // 可以根据状态启动/停止动画
            }
        }

        private void SetupMotorAnimation()
        {
            // 为散热片创建旋转动画（模拟电机旋转）
            _motorStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

_finAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            // 如果有散热片动画，这里可以添加
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
            return new Rect(0, 0, 160, 280);
        }
    }
}

