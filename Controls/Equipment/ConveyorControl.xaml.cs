using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class ConveyorControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty BeltSpeedProperty =
            DependencyProperty.Register(nameof(BeltSpeed), typeof(double), typeof(ConveyorControl),
                new PropertyMetadata(0.0, OnBeltSpeedChanged));

        public static readonly DependencyProperty MotorStateProperty =
            DependencyProperty.Register(nameof(MotorState), typeof(MotorState), typeof(ConveyorControl),
                new PropertyMetadata(MotorState.Stopped, OnMotorStateChanged));

        public static readonly DependencyProperty HasRubberBlocksProperty =
            DependencyProperty.Register(nameof(HasRubberBlocks), typeof(bool), typeof(ConveyorControl),
                new PropertyMetadata(true));

        public double BeltSpeed
        {
            get => (double)GetValue(BeltSpeedProperty);
            set => SetValue(BeltSpeedProperty, value);
        }

        public MotorState MotorState
        {
            get => (MotorState)GetValue(MotorStateProperty);
            set => SetValue(MotorStateProperty, value);
        }

        public bool HasRubberBlocks
        {
            get => (bool)GetValue(HasRubberBlocksProperty);
            set => SetValue(HasRubberBlocksProperty, value);
        }

        private Storyboard _beltStoryboard;
        private DoubleAnimation _beltOffsetAnimation;
        private ObservableCollection<RubberBlock> _rubberBlocks = new();

        public ObservableCollection<RubberBlock> RubberBlocks => _rubberBlocks;

        public ConveyorControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
            if (RubberBlocksControl != null) RubberBlocksControl.ItemsSource = _rubberBlocks;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetupBeltAnimation();
            GenerateRubberBlocks();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnBeltSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 更新皮带动画速度
        }

        private static void OnMotorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConveyorControl control)
            {
                var state = (MotorState)e.NewValue;
                switch (state)
                {
                    case MotorState.Running:
                        control.StartAnimation();
                        break;
                    case MotorState.Stopped:
                    case MotorState.Fault:
                        control.StopAnimation();
                        break;
                }
            }
        }

        private void SetupBeltAnimation()
        {
            _beltStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

_beltOffsetAnimation = new DoubleAnimation
            {
                From = 0,
                To = -100,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            if (Belt != null)
            {
                Storyboard.SetTarget(_beltOffsetAnimation, Belt);
                Storyboard.SetTargetProperty(_beltOffsetAnimation, new PropertyPath("(Path.Fill).(Brush.RelativeTransform).(TranslateTransform.X)"));

                _beltStoryboard.Children.Add(_beltOffsetAnimation);
            }
        }

        private void GenerateRubberBlocks()
        {
            _rubberBlocks.Clear();
            if (!HasRubberBlocks) return;

            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                _rubberBlocks.Add(new RubberBlock
                {
                    X = random.Next(20, 200),
                    Y = 0
                });
            }
        }

        private void StartAnimation()
        {
            _beltStoryboard?.Begin(this, true);
        }

        private void StopAnimation()
        {
            // 停止动画
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
            return new Rect(0, 0, 300, 70);
        }

        public class RubberBlock
        {
            public double X { get; set; }
            public double Y { get; set; }
        }
    }
}

