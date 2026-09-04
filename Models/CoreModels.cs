using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    public enum EquipmentType
    {
        // 输送部分
        RootBlower,
        DustCollector,
        FeedBin,
        PressurizedTank,
        SideBlowValve,
        ThreeWayValve,
        TwoWayValve,
        
        // 上辅机部分
        CarbonBlackTank,
        OilTank,
        CarbonBlackScale,
        OilScale,
        Mixer,
        RubberScale,
        RubberConveyor,
        OilPump,
        
        // 管路
        PipeStraight,
        PipeElbow90,
        PipeTee,
        PipeReducer,
        SideBlowDevice
    }

    public enum EquipmentState
    {
        Stopped = 0,
        Running = 1,
        Warning = 2,
        Alarm = 3,
        Maintenance = 4
    }

    public enum PipeType
    {
        None,
        CarbonConveying,   // 炭黑输送
        OilSupply,         // 供油管
        CoolingWater,      // 冷却水
        CompressedAir,     // 压缩空气
        DustCollection     // 除尘管
    }

    public enum FlowDirection
    {
        Forward,
        Reverse
    }

    public enum AnchorType
    {
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom
    }

    public enum ValveState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    /// <summary>
    /// 连接点定义
    /// </summary>
    public class AnchorPoint : INotifyPropertyChanged
    {
        private Point _position;
        private bool _isConnected;
        private string _connectedPipeId;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public AnchorType Type { get; set; }
        
        public Point Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); }
        }

        public Point RelativePosition { get; set; }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); }
        }

        public string ConnectedPipeId
        {
            get => _connectedPipeId;
            set { _connectedPipeId = value; OnPropertyChanged(nameof(ConnectedPipeId)); }
        }

        public string OwnerElementId { get; set; }
        public double SnapRadius { get; set; } = 15;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 管道段基础模型
    /// </summary>
    public abstract class PipeSegment : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public PipeType PipeType { get; set; }
        
        private Point _startPoint;
        private Point _endPoint;
        private ObservableCollection<Point> _controlPoints = new();
        private double _diameter = 50;
        private double _flowSpeed = 1.0;
        private FlowDirection _flowDirection = FlowDirection.Forward;
        private bool _isAnimating = false;

        public Point StartPoint
        {
            get => _startPoint;
            set { _startPoint = value; OnPropertyChanged(nameof(StartPoint)); UpdateGeometry(); }
        }

        public Point EndPoint
        {
            get => _endPoint;
            set { _endPoint = value; OnPropertyChanged(nameof(EndPoint)); UpdateGeometry(); }
        }

        public ObservableCollection<Point> ControlPoints
        {
            get => _controlPoints;
            set { _controlPoints = value; OnPropertyChanged(nameof(ControlPoints)); }
        }

        public virtual Point? ControlPoint1
        {
            get => ControlPoints.Count > 0 ? ControlPoints[0] : (Point?)null;
            set
            {
                if (value.HasValue)
                {
                    if (ControlPoints.Count > 0) ControlPoints[0] = value.Value;
                    else ControlPoints.Add(value.Value);
                }
                else if (ControlPoints.Count > 0)
                {
                    ControlPoints.RemoveAt(0);
                }
                OnPropertyChanged(nameof(ControlPoint1));
            }
        }

        public virtual Point? ControlPoint2
        {
            get => ControlPoints.Count > 1 ? ControlPoints[1] : (Point?)null;
            set
            {
                if (value.HasValue)
                {
                    if (ControlPoints.Count > 1) ControlPoints[1] = value.Value;
                    else if (ControlPoints.Count == 1) ControlPoints.Add(value.Value);
                    else { ControlPoints.Add(default); ControlPoints.Add(value.Value); }
                }
                else if (ControlPoints.Count > 1)
                {
                    ControlPoints.RemoveAt(1);
                }
                OnPropertyChanged(nameof(ControlPoint2));
            }
        }

        public double Diameter
        {
            get => _diameter;
            set { _diameter = Math.Max(10, Math.Min(200, value)); OnPropertyChanged(nameof(Diameter)); UpdateGeometry(); }
        }

        public double FlowSpeed
        {
            get => _flowSpeed;
            set { _flowSpeed = Math.Max(0, value); OnPropertyChanged(nameof(FlowSpeed)); }
        }

        public FlowDirection FlowDirection
        {
            get => _flowDirection;
            set { _flowDirection = value; OnPropertyChanged(nameof(FlowDirection)); }
        }

        public bool IsAnimating
        {
            get => _isAnimating;
            set { _isAnimating = value; OnPropertyChanged(nameof(IsAnimating)); }
        }

        public string StartAnchorId { get; set; }
        public string EndAnchorId { get; set; }

        protected abstract void UpdateGeometry();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 直管
    /// </summary>
    public class StraightPipe : PipeSegment
    {
        public double Length => Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
        public double Angle => Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X) * 180 / Math.PI;

        protected override void UpdateGeometry()
        {
            OnPropertyChanged(nameof(Length));
            OnPropertyChanged(nameof(Angle));
        }
    }

    /// <summary>
    /// 90度弯管
    /// </summary>
    public class Elbow90Pipe : PipeSegment
    {
        public ElbowDirection Direction { get; set; } = ElbowDirection.TopRight;
        public double Radius { get; set; } = 60;

        public enum ElbowDirection
        {
            TopLeft,    // 左上
            TopRight,   // 右上
            BottomLeft, // 左下
            BottomRight // 右下
        }

        protected override void UpdateGeometry()
        {
            // 根据方向计算控制点
            ControlPoints.Clear();
            var dx = EndPoint.X - StartPoint.X;
            var dy = EndPoint.Y - StartPoint.Y;
            
            switch (Direction)
            {
                case ElbowDirection.TopRight:
                    ControlPoints.Add(new Point(EndPoint.X, StartPoint.Y));
                    break;
                case ElbowDirection.TopLeft:
                    ControlPoints.Add(new Point(StartPoint.X, EndPoint.Y));
                    break;
                case ElbowDirection.BottomRight:
                    ControlPoints.Add(new Point(StartPoint.X, EndPoint.Y));
                    break;
                case ElbowDirection.BottomLeft:
                    ControlPoints.Add(new Point(EndPoint.X, StartPoint.Y));
                    break;
            }
        }
    }

    /// <summary>
    /// 三通管
    /// </summary>
    public class TeePipe : PipeSegment
    {
        public Point BranchPoint { get; set; }
        public TeeDirection Direction { get; set; } = TeeDirection.Horizontal;

        public enum TeeDirection
        {
            Horizontal,  // 水平主管 + 竖向分支
            Vertical     // 竖向主管 + 水平分支
        }

        protected override void UpdateGeometry()
        {
            // 三通有三个端点：StartPoint, EndPoint, BranchPoint
        }
    }

    /// <summary>
    /// 管道模型 - 用于UI绑定的管道段封装
    /// </summary>
    public class PipeModel : PipeSegment
    {
        protected override void UpdateGeometry()
        {
            // 由子类实现具体几何更新
        }
    }

    /// <summary>
    /// 设备基础模型
    /// </summary>
    public abstract class EquipmentBase : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public EquipmentType Type { get; protected set; }

        private Point _position;
        private double _rotation;
        private double _width;
        private double _height;
        private EquipmentState _state;
        private bool _isSelected;
        private ObservableCollection<AnchorPoint> _anchors = new();

        public Point Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); UpdateAnchors(); }
        }

        public double Rotation
        {
            get => _rotation;
            set { _rotation = value; OnPropertyChanged(nameof(Rotation)); UpdateAnchors(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(nameof(Width)); UpdateAnchors(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(nameof(Height)); UpdateAnchors(); }
        }

        public EquipmentState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(nameof(State)); OnPropertyChanged(nameof(StateColor)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public ObservableCollection<AnchorPoint> Anchors => _anchors;

        public Dictionary<string, string> TagBindings { get; set; } = new();

        public virtual Color StateColor
        {
            get
            {
                return State switch
                {
                    EquipmentState.Stopped => Colors.Gray,
                    EquipmentState.Running => Colors.LimeGreen,
                    EquipmentState.Warning => Colors.Gold,
                    EquipmentState.Alarm => Colors.Red,
                    EquipmentState.Maintenance => Colors.Blue,
                    _ => Colors.Gray
                };
            }
        }

        protected virtual void UpdateAnchors()
        {
            foreach (var anchor in _anchors)
            {
                if (anchor.RelativePosition.X >= 0 && anchor.RelativePosition.X <= 1 &&
                    anchor.RelativePosition.Y >= 0 && anchor.RelativePosition.Y <= 1)
                {
                    anchor.Position = new Point(
                        Position.X + anchor.RelativePosition.X * Width,
                        Position.Y + anchor.RelativePosition.Y * Height);
                }
            }
        }

        public abstract Rect GetBounds();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 组态文档模型
    /// </summary>
    public class ProjectDocument : INotifyPropertyChanged
    {
        public string Version { get; set; } = "1.0";
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime ModifiedTime { get; set; } = DateTime.Now;
        
        public double CanvasWidth { get; set; } = 1920;
        public double CanvasHeight { get; set; } = 1080;
        public double GridSize { get; set; } = 20;
        public bool SnapToGrid { get; set; } = true;
        public bool ShowGrid { get; set; } = true;

        public ObservableCollection<EquipmentBase> Equipments { get; set; } = new();
        public ObservableCollection<PipeSegment> Pipes { get; set; } = new();
        
        // 连接关系
        public ObservableCollection<ConnectionInfo> Connections { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ConnectionInfo : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromElementId { get; set; }
        public string FromAnchorId { get; set; }
        public string ToElementId { get; set; }
        public string ToAnchorId { get; set; }
        public string PipeId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}