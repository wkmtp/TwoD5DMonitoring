using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TwoD5DMonitoring.Models;
using ModelsFlowDirection = TwoD5DMonitoring.Models.FlowDirection;

namespace TwoD5DMonitoring.Core
{
    /// <summary>
    /// 设备控件基础接口
    /// </summary>
    public interface IEquipmentControl
    {
        string Id { get; }
        EquipmentType Type { get; }
        EquipmentConfig Config { get; set; }
        EquipmentState State { get; set; }
        
        // 连接点
        IEnumerable<AnchorPointInfo> AnchorPoints { get; }
        Point GetAnchorPosition(AnchorPoint anchor);
        
        // 状态与数据
        void UpdateState(EquipmentState state);
        void UpdateTagValue(string tagName, object value);
        
        // 序列化
        EquipmentConfig ToConfig();
        void FromConfig(EquipmentConfig config);
        
        // 交互
        void Select();
        void Deselect();
        bool IsSelected { get; }
        
        // 连接点吸附
        double SnapThreshold { get; }
    }

    /// <summary>
    /// 连接点信息
    /// </summary>
    public class AnchorPointInfo
    {
        public AnchorPoint Anchor { get; set; }
        public Point Position { get; set; }
        public string EquipmentId { get; set; }
        public bool IsConnected { get; set; }
        public string ConnectedPipeId { get; set; }
    }

    /// <summary>
    /// 管路控件接口
    /// </summary>
    public interface IPipeControl
    {
        string Id { get; }
        PipeConfig Config { get; set; }
        
        // 路径几何
        PathGeometry PathGeometry { get; }
        void UpdatePath();
        
        // 连接端点
        Point StartPoint { get; set; }
        Point EndPoint { get; set; }
        ObservableCollection<Point> ControlPoints { get; }
        
        // 动画
        void StartFlowAnimation();
        void StopFlowAnimation();
        void SetFlowSpeed(double speed);
        void SetFlowDirection(ModelsFlowDirection direction);
        
        // 阀门
        void AddValve(string valveId, Point position);
        void RemoveValve(string valveId);
        
        // 序列化
        PipeConfig ToConfig();
        void FromConfig(PipeConfig config);
    }

    /// <summary>
    /// 设计器画布接口
    /// </summary>
    public interface IDesignerCanvas
    {
        // 网格
        double GridSize { get; set; }
        bool SnapToGrid { get; set; }
        void ShowGrid(bool show);
        
        // 设备管理
        void AddEquipment(IEquipmentControl equipment);
        void RemoveEquipment(IEquipmentControl equipment);
        IEquipmentControl GetEquipment(string id);
        IEnumerable<IEquipmentControl> GetAllEquipments();
        
        // 管路管理
        void AddPipe(IPipeControl pipe);
        void RemovePipe(IPipeControl pipe);
        IPipeControl GetPipe(string id);
        
        // 连接管理
        void CreateConnection(string fromEquipId, AnchorPoint fromAnchor, string toEquipId, AnchorPoint toAnchor);
        void RemoveConnection(string connectionId);
        
        // 选择与交互
        event Action<IEquipmentControl> EquipmentSelected;
        event Action<IPipeControl> PipeSelected;
        event Action SelectionChanged;
        
        IEquipmentControl SelectedEquipment { get; }
        IPipeControl SelectedPipe { get; }
        
        // 撤销重做
        void Undo();
        void Redo();
        bool CanUndo { get; }
        bool CanRedo { get; }
        
        // 序列化
        ConfigModel Save();
        void Load(ConfigModel config);
    }

    /// <summary>
    /// 运行时视图接口
    /// </summary>
    public interface IRuntimeView
    {
        void LoadConfig(ConfigModel config);
        void BindPlcData(IDictionary<string, object> plcData);
        void Start();
        void Stop();
        
        event Action<string, object> TagValueChanged;
    }
}