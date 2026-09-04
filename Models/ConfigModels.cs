using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Models
{
    /// <summary>
    /// 组态文件根模型
    /// </summary>
    public class ConfigModel
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("canvasWidth")]
        public double CanvasWidth { get; set; } = 1920;

        [JsonProperty("canvasHeight")]
        public double CanvasHeight { get; set; } = 1080;

        [JsonProperty("gridSize")]
        public double GridSize { get; set; } = 20;

        [JsonProperty("snapToGrid")]
        public bool SnapToGrid { get; set; } = true;

        [JsonProperty("equipments", ItemTypeNameHandling = TypeNameHandling.Auto)]
        public ObservableCollection<EquipmentConfig> Equipments { get; set; } = new();

        [JsonProperty("pipes", ItemTypeNameHandling = TypeNameHandling.Auto)]
        public ObservableCollection<PipeConfig> Pipes { get; set; } = new();

        [JsonProperty("connections", ItemTypeNameHandling = TypeNameHandling.Auto)]
        public ObservableCollection<ConnectionConfig> Connections { get; set; } = new();
    }

    /// <summary>
    /// 设备配置基类
    /// </summary>
    public abstract class EquipmentConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("type")]
        public abstract EquipmentType Type { get; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; } = 100;

        [JsonProperty("height")]
        public double Height { get; set; } = 100;

        [JsonProperty("rotation")]
        public double Rotation { get; set; } = 0;

        [JsonProperty("zIndex")]
        public int ZIndex { get; set; } = 0;

        [JsonProperty("isVisible")]
        public bool IsVisible { get; set; } = true;

        [JsonProperty("tagBindings")]
        public Dictionary<string, string> TagBindings { get; set; } = new();

        [JsonProperty("customProperties")]
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }

    /// <summary>
    /// 具体设备配置类型
    /// </summary>
    public class RootBlowerConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.RootBlower;
        
        [JsonProperty("blowerStyle")]
        public BlowerStyle Style { get; set; } = BlowerStyle.Standard;

        [JsonProperty("impellerCount")]
        public int ImpellerCount { get; set; } = 2;
    }

    public class DustCollectorConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.DustCollector;
        
        [JsonProperty("hasMotor")]
        public bool HasMotor { get; set; } = true;

        [JsonProperty("motorOnTop")]
        public bool MotorOnTop { get; set; } = true;

        [JsonProperty("pipeInterfacePosition")]
        public InterfacePosition PipeInterface { get; set; } = InterfacePosition.Top;
    }

    public class FeedBinConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.FeedBin;
        
        [JsonProperty("binShape")]
        public BinShape Shape { get; set; } = BinShape.Trapezoid;

        [JsonProperty("hasDoor")]
        public bool HasDoor { get; set; } = true;

        [JsonProperty("doorAnimationDuration")]
        public double DoorAnimationDuration { get; set; } = 1.5;

        [JsonProperty("dustOutletPosition")]
        public InterfacePosition DustOutlet { get; set; } = InterfacePosition.Top;
    }

    public class PressurizedTankConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.PressurizedTank;
        
        [JsonProperty("tankShape")]
        public TankShape Shape { get; set; } = TankShape.Conical;

        [JsonProperty("levelSensorCount")]
        public int LevelSensorCount { get; set; } = 1;

        [JsonProperty("hasTopInlet")]
        public bool HasTopInlet { get; set; } = true;

        [JsonProperty("hasBottomOutlet")]
        public bool HasBottomOutlet { get; set; } = true;

        [JsonProperty("hasSolenoidValve")]
        public bool HasSolenoidValve { get; set; } = true;
    }

    public class CarbonBlackTankConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.CarbonBlackTank;
        
        [JsonProperty("tankMaterial")]
        public TankConstructionMaterial Material { get; set; } = TankConstructionMaterial.CarbonSteel;

        [JsonProperty("hasRecoveryTank")]
        public bool HasRecoveryTank { get; set; } = false;

        [JsonProperty("tankShape")]
        public TankShape Shape { get; set; } = TankShape.CylinderCone;
    }

    public class OilTankConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.OilTank;
        
        [JsonProperty("tankMaterial")]
        public TankConstructionMaterial Material { get; set; } = TankConstructionMaterial.StainlessSteel;

        [JsonProperty("hasTemperatureLabel")]
        public bool HasTemperatureLabel { get; set; } = true;

        [JsonProperty("hasMaterialNameLabel")]
        public bool HasMaterialNameLabel { get; set; } = true;
    }

    public class CarbonBlackScaleConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.CarbonBlackScale;
        
        [JsonProperty("scaleShape")]
        public ScaleShape Shape { get; set; } = ScaleShape.Conical;

        [JsonProperty("connectedTankCount")]
        public int ConnectedTankCount { get; set; } = 1;

        [JsonProperty("hasDischargeValve")]
        public bool HasDischargeValve { get; set; } = true;
    }

    public class OilScaleConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.OilScale;
        
        [JsonProperty("scaleShape")]
        public ScaleShape Shape { get; set; } = ScaleShape.Conical;
    }

    public class MixerConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.Mixer;
        
        [JsonProperty("hasFeedDoor")]
        public bool HasFeedDoor { get; set; } = true;

        [JsonProperty("hasDischargeDoor")]
        public bool HasDischargeDoor { get; set; } = true;

        [JsonProperty("hasRotorAnimation")]
        public bool HasRotorAnimation { get; set; } = true;

        [JsonProperty("parameterPanelPosition")]
        public PanelPosition ParameterPanel { get; set; } = PanelPosition.Right;
    }

    public class RubberScaleConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.RubberScale;
        
        [JsonProperty("scaleShape")]
        public ScaleShape Shape { get; set; } = ScaleShape.Conical;
    }

    public class RubberConveyorConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.RubberConveyor;
        
        [JsonProperty("beltWidth")]
        public double BeltWidth { get; set; } = 800;

        [JsonProperty("hasAnimation")]
        public bool HasAnimation { get; set; } = true;
    }

    public class OilPumpConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.OilPump;
        
        [JsonProperty("pumpStyle")]
        public PumpStyle Style { get; set; } = PumpStyle.Standard;

        [JsonProperty("hasFlowIndicator")]
        public bool HasFlowIndicator { get; set; } = true;
    }

    public class PipeReducerConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.PipeReducer;
        
        [JsonProperty("reducerType")]
        public ReducerType ReducerType { get; set; } = ReducerType.Concentric;

        [JsonProperty("inletDiameter")]
        public double InletDiameter { get; set; } = 100;

        [JsonProperty("outletDiameter")]
        public double OutletDiameter { get; set; } = 50;

        [JsonProperty("length")]
        public double Length { get; set; } = 200;
    }

    public class SideBlowDeviceConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.SideBlowDevice;
        
        [JsonProperty("mainDiameter")]
        public double MainDiameter { get; set; } = 100;

        [JsonProperty("branchDiameter")]
        public double BranchDiameter { get; set; } = 50;

        [JsonProperty("branchAngle")]
        public double BranchAngle { get; set; } = 45;

        [JsonProperty("valveState")]
        public ValveState ValveState { get; set; } = ValveState.Closed;
    }

    public class SideBlowValveConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.SideBlowValve;
        
        [JsonProperty("valveType")]
        public ValveType ValveType { get; set; } = ValveType.Ball;

        [JsonProperty("diameter")]
        public double Diameter { get; set; } = 50;

        [JsonProperty("hasPositionFeedback")]
        public bool HasPositionFeedback { get; set; } = true;
    }

    public class ThreeWayValveConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.ThreeWayValve;
        
        [JsonProperty("valveType")]
        public ValveType ValveType { get; set; } = ValveType.Ball;

        [JsonProperty("portDiameter")]
        public double PortDiameter { get; set; } = 50;

        [JsonProperty("flowPattern")]
        public string FlowPattern { get; set; } = "L-Port";
    }

    public class TwoWayValveConfig : EquipmentConfig
    {
        public override EquipmentType Type => EquipmentType.TwoWayValve;
        
        [JsonProperty("valveType")]
        public ValveType ValveType { get; set; } = ValveType.Ball;

        [JsonProperty("diameter")]
        public double Diameter { get; set; } = 50;

        [JsonProperty("isNormallyOpen")]
        public bool IsNormallyOpen { get; set; } = false;
    }

    /// <summary>
    /// 管路配置
    /// </summary>
    public class PipeConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("type")]
        public PipeType Type { get; set; } = PipeType.CarbonConveying;

        [JsonProperty("startPoint")]
        public PointConfig StartPoint { get; set; } = new();

        [JsonProperty("endPoint")]
        public PointConfig EndPoint { get; set; } = new();

        [JsonProperty("controlPoints")]
        public ObservableCollection<PointConfig> ControlPoints { get; set; } = new();

        [JsonProperty("diameter")]
        public double Diameter { get; set; } = 50;

        [JsonProperty("length")]
        public double Length { get; set; } = 0;

        [JsonProperty("material")]
        public PipeMaterial Material { get; set; } = PipeMaterial.CarbonSteel;

        [JsonProperty("color")]
        public string Color { get; set; } = "#888888";

        [JsonProperty("dashPattern")]
        public string DashPattern { get; set; } = "";

        [JsonProperty("flowDirection")]
        public FlowDirection FlowDirection { get; set; } = FlowDirection.Forward;

        [JsonProperty("flowSpeed")]
        public double FlowSpeed { get; set; } = 1.0;

        [JsonProperty("hasAnimation")]
        public bool HasAnimation { get; set; } = true;

        [JsonProperty("valveIds")]
        public ObservableCollection<string> ValveIds { get; set; } = new();

        [JsonProperty("tagBinding")]
        public string TagBinding { get; set; } = "";
    }

    /// <summary>
    /// 连接配置（设备间的管路连接关系）
    /// </summary>
    public class ConnectionConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("fromEquipmentId")]
        public string FromEquipmentId { get; set; } = "";

        [JsonProperty("fromAnchor")]
        public Models.AnchorType FromAnchor { get; set; } = Models.AnchorType.Right;

        [JsonProperty("toEquipmentId")]
        public string ToEquipmentId { get; set; } = "";

        [JsonProperty("toAnchor")]
        public Models.AnchorType ToAnchor { get; set; } = Models.AnchorType.Left;

        [JsonProperty("pipeId")]
        public string PipeId { get; set; } = "";
    }

    /// <summary>
    /// 点坐标配置
    /// </summary>
    public class PointConfig
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }
    }

    // ============= 枚举定义（仅配置特有的，不与 CoreModels 重复） =============

    public enum BlowerStyle { Standard, HighPressure }
    public enum InterfacePosition { Top, Bottom, Left, Right }
    public enum BinShape { Trapezoid, Rectangle, Circular }
    public enum TankShape { Conical, CylinderCone, Cylinder, Rectangle }
    public enum TankConstructionMaterial { CarbonSteel, StainlessSteel, Plastic }
    public enum ScaleShape { Conical, Rectangle, Circular }
    public enum PumpStyle { Standard, HighFlow }
    public enum PanelPosition { Left, Right, Top, Bottom }
    public enum PipeMaterial { CarbonSteel, StainlessSteel, Plastic, Rubber }
    public enum ConfigAnchorPoint { Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight, Center }
    public enum ReducerType { Concentric, Eccentric }
}