using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Controls.Equipment
{
    public partial class PipeReducerControl : BaseEquipmentControl
    {
        public static readonly DependencyProperty ReducerTypeProperty =
            DependencyProperty.Register(nameof(ReducerType), typeof(ReducerType), typeof(PipeReducerControl),
                new PropertyMetadata(ReducerType.Concentric, OnReducerTypeChanged));

        public ReducerType ReducerType
        {
            get => (ReducerType)GetValue(ReducerTypeProperty);
            set => SetValue(ReducerTypeProperty, value);
        }

        public PipeReducerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateReducerType();
            UpdateAnchors();
            UpdateAnchorsVisibility();
        }

        private static void OnReducerTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PipeReducerControl control)
                control.UpdateReducerType();
        }

        private void UpdateReducerType()
        {
            if (ConcentricReducer != null && EccentricReducer != null)
            {
                ConcentricReducer.Visibility = ReducerType == ReducerType.Concentric ? Visibility.Visible : Visibility.Collapsed;
                EccentricReducer.Visibility = ReducerType == ReducerType.Eccentric ? Visibility.Visible : Visibility.Collapsed;
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