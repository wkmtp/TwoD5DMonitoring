using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using TwoD5DMonitoring.Models;
using ModelsValveType = TwoD5DMonitoring.Models.ValveType;
using ModelsValveFlowDirection = TwoD5DMonitoring.Models.ValveFlowDirection;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class ValveControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty ValveTypeProperty =
            DependencyProperty.Register(nameof(ValveType), typeof(ModelsValveType), typeof(ValveControl),
                new PropertyMetadata(ModelsValveType.YType, OnValveTypeChanged));

        public static readonly DependencyProperty ValveStateProperty =
            DependencyProperty.Register(nameof(ValveState), typeof(ValveState), typeof(ValveControl),
                new PropertyMetadata(ValveState.Closed, OnValveStateChanged));

        public static readonly DependencyProperty FlowDirectionProperty =
            DependencyProperty.Register(nameof(FlowDirection), typeof(ModelsValveFlowDirection), typeof(ValveControl),
                new PropertyMetadata(ModelsValveFlowDirection.Straight));

        public ModelsValveType ValveType
        {
            get => (ModelsValveType)GetValue(ValveTypeProperty);
            set => SetValue(ValveTypeProperty, value);
        }

        public ValveState ValveState
        {
            get => (ValveState)GetValue(ValveStateProperty);
            set => SetValue(ValveStateProperty, value);
        }

        public ModelsValveFlowDirection FlowDirection
        {
            get => (ModelsValveFlowDirection)GetValue(FlowDirectionProperty);
            set => SetValue(FlowDirectionProperty, value);
        }

        public ValveControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateValveVisual();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnValveTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValveControl control)
                control.UpdateValveVisual();
        }

        private static void OnValveStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValveControl control)
                control.UpdateValveAnimation();
        }

        private void UpdateValveVisual()
        {
            if (ValveType == ModelsValveType.YType)
            {
                if (FlowPath != null && Resources["YTypeValveGeometry"] is PathGeometry geom)
                    FlowPath.Data = geom;
            }
            else
            {
                if (FlowPath != null && Resources["VTypeValveGeometry"] is PathGeometry geom)
                    FlowPath.Data = geom;
            }
        }

        private void UpdateValveAnimation()
        {
            if (CoreTranslate != null)
            {
                var targetX = ValveState switch
                {
                    ValveState.Closed => 15,
                    ValveState.Opening => 10,
                    ValveState.Open => 5,
                    ValveState.Closing => 10,
                    _ => 15
                };

                var animation = new DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                CoreTranslate.BeginAnimation(TranslateTransform.XProperty, animation);
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
            return new Rect(0, 0, 50, 80);
        }
    }
}

