using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    // 设备具体模型
    public class RootBlowerModel : EquipmentBase
    {
        public RootBlowerModel()
        {
            Type = EquipmentType.RootBlower;
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出气口", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进气口", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
        }

        public double RotorSpeed { get; set; }
        public MotorState MotorState { get; set; }
        public int ImpellerCount { get; set; } = 2;

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class DustCollectorModel : EquipmentBase
    {
        public DustCollectorModel()
        {
            Type = EquipmentType.DustCollector;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进风口", Type = AnchorType.Left, RelativePosition = new Point(0, 0.3) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出风口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "dust", Name = "排灰口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public MotorState MotorState { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class FeedBinModel : EquipmentBase
    {
        public FeedBinModel()
        {
            Type = EquipmentType.FeedBin;
            Anchors.Add(new AnchorPoint { Id = "dust", Name = "除尘口", Type = AnchorType.Top, RelativePosition = new Point(0.2, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public DoorState DoorState { get; set; }
        public double FillRatio { get; set; }
        public double DustLevel { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class PressurizedTankModel : EquipmentBase
    {
        public PressurizedTankModel()
        {
            Type = EquipmentType.PressurizedTank;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
            Anchors.Add(new AnchorPoint { Id = "dust", Name = "除尘口", Type = AnchorType.Top, RelativePosition = new Point(0.2, 0) });
        }

        public double FillRatio { get; set; }
        public bool InletValveOpen { get; set; }
        public bool OutletValveOpen { get; set; }
        public bool HighLevelState { get; set; }
        public bool LowLevelState { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class CarbonBlackTankModel : EquipmentBase
    {
        public CarbonBlackTankModel()
        {
            Type = EquipmentType.CarbonBlackTank;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public double FillRatio { get; set; }
        public double Temperature { get; set; }
        public bool HighLevelState { get; set; }
        public bool LowLevelState { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class OilTankModel : EquipmentBase
    {
        public OilTankModel()
        {
            Type = EquipmentType.OilTank;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public double FillRatio { get; set; }
        public double Temperature { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class MixerModel : EquipmentBase
    {
        public MixerModel()
        {
            Type = EquipmentType.Mixer;
            Anchors.Add(new AnchorPoint { Id = "feed", Name = "加料口", Type = AnchorType.Top, RelativePosition = new Point(0.3, 0) });
            Anchors.Add(new AnchorPoint { Id = "discharge", Name = "卸料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.7, 1) });
        }

        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double Speed { get; set; }
        public double Power { get; set; }
        public double Energy { get; set; }
        public DoorState FeedDoorState { get; set; }
        public DoorState DischargeDoorState { get; set; }
        public double RotorAnimationSpeed { get; set; } = 1.0;

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class CarbonBlackScaleModel : EquipmentBase
    {
        public CarbonBlackScaleModel()
        {
            Type = EquipmentType.CarbonBlackScale;
            Anchors.Add(new AnchorPoint { Id = "inlet1", Name = "进料1", Type = AnchorType.Top, RelativePosition = new Point(0.3, 0) });
            Anchors.Add(new AnchorPoint { Id = "inlet2", Name = "进料2", Type = AnchorType.Top, RelativePosition = new Point(0.7, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public double Weight { get; set; }
        public double FillRatio { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class OilScaleModel : EquipmentBase
    {
        public OilScaleModel()
        {
            Type = EquipmentType.OilScale;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public double Weight { get; set; }
        public double FillRatio { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class RubberScaleModel : EquipmentBase
    {
        public RubberScaleModel()
        {
            Type = EquipmentType.RubberScale;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料口", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
        }

        public double Weight { get; set; }
        public double FillRatio { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class RubberConveyorModel : EquipmentBase
    {
        public RubberConveyorModel()
        {
            Type = EquipmentType.RubberConveyor;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进料端", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出料端", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
        }

        public double BeltSpeed { get; set; }
        public MotorState MotorState { get; set; }
        public bool HasRubberBlocks { get; set; } = true;

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class OilPumpModel : EquipmentBase
    {
        public OilPumpModel()
        {
            Type = EquipmentType.OilPump;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进口", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出口", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
        }

        public double RotorSpeed { get; set; }
        public MotorState MotorState { get; set; }
        public FlowDirection FlowDirection { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    // 管路阀门模型
    public class SideBlowValveModel : EquipmentBase
    {
        public SideBlowValveModel()
        {
            Type = EquipmentType.SideBlowValve;
            Anchors.Add(new AnchorPoint { Id = "gas", Name = "气体管", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "material", Name = "物料管", Type = AnchorType.Bottom, RelativePosition = new Point(0.5, 1) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出口", Type = AnchorType.Right, RelativePosition = new Point(1, 0.3) });
        }

        public double ValveAngle { get; set; } = 45;
        public ValveState ValveState { get; set; }
        public double BlowAngle { get; set; } = 45;

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class ThreeWayValveModel : EquipmentBase
    {
        public ThreeWayValveModel()
        {
            Type = EquipmentType.ThreeWayValve;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
            Anchors.Add(new AnchorPoint { Id = "outlet1", Name = "出口1", Type = AnchorType.BottomLeft, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "outlet2", Name = "出口2", Type = AnchorType.BottomRight, RelativePosition = new Point(1, 0.5) });
        }

        public ValveState ValveState { get; set; }
        public ValveFlowDirection FlowDirection { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class TwoWayValveModel : EquipmentBase
    {
        public TwoWayValveModel()
        {
            Type = EquipmentType.TwoWayValve;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "进口", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "出口", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
        }

        public ValveState ValveState { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class PipeReducerModel : EquipmentBase
    {
        public PipeReducerModel()
        {
            Type = EquipmentType.PipeReducer;
            Anchors.Add(new AnchorPoint { Id = "inlet", Name = "大口径端", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "outlet", Name = "小口径端", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
        }

        public double InletDiameter { get; set; } = 100;
        public double OutletDiameter { get; set; } = 50;
        public double Length { get; set; } = 200;
        public ReducerType ReducerType { get; set; } = ReducerType.Concentric;

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    public class SideBlowDeviceModel : EquipmentBase
    {
        public SideBlowDeviceModel()
        {
            Type = EquipmentType.SideBlowDevice;
            Anchors.Add(new AnchorPoint { Id = "mainInlet", Name = "主管进口", Type = AnchorType.Left, RelativePosition = new Point(0, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "mainOutlet", Name = "主管出口", Type = AnchorType.Right, RelativePosition = new Point(1, 0.5) });
            Anchors.Add(new AnchorPoint { Id = "branch", Name = "旁路出口", Type = AnchorType.Top, RelativePosition = new Point(0.5, 0) });
        }

        public double MainDiameter { get; set; } = 100;
        public double BranchDiameter { get; set; } = 50;
        public double BranchAngle { get; set; } = 45;
        public ValveState ValveState { get; set; }

        public override Rect GetBounds() => new Rect(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
    }

    // 辅助枚举 - 只定义 CoreModels.cs 中没有的
    public enum DoorState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    public enum ValveFlowDirection
    {
        Straight,
        Diverted,
        Mixed
    }

    public enum MotorState
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Fault
    }

    public enum ScaleType
    {
        CarbonBlack,
        Oil,
        Rubber
    }

    public enum ValveType
    {
        Ball,     // 球阀
        YType,    // Y型三通
        VType     // V型两通
    }

    public enum TankType
    {
        CarbonBlack,
        Oil,
        Rubber
    }
}