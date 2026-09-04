using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Controls;
using TwoD5DMonitoring.Controls.Equipment;
using TwoD5DMonitoring.Controls.Pipes;

namespace TwoD5DMonitoring.Runtime
{
    public partial class RuntimeView : UserControl, INotifyPropertyChanged
    {
        private ProjectDocument _project;
        private Dictionary<string, BaseEquipmentControl> _elementMap = new();
        private Dictionary<string, PipeControl> _pipeMap = new();
        private DispatcherTimer _updateTimer;
        private DispatcherTimer _tagSimulationTimer;
        private Dictionary<string, object> _simulatedTagValues = new();
        private Random _random = new Random();

        public RuntimeView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _updateTimer.Tick += OnUpdateTimer;
            _updateTimer.Start();

            // 标签值模拟器 - 每秒更新一次
            _tagSimulationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _tagSimulationTimer.Tick += OnTagSimulationTick;
            _tagSimulationTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            _tagSimulationTimer?.Stop();
        }

        public void LoadProject(ProjectDocument project)
        {
            _project = project;
            BuildRuntimeView();
            InitializeTagSimulator();
        }

        private void BuildRuntimeView()
        {
            if (_project == null) return;

            RuntimeCanvas.Children.Clear();
            _elementMap.Clear();
            _pipeMap.Clear();

            // 创建设备元素
            foreach (var equip in _project.Equipments)
            {
                var ctrl = CreateRuntimeControl(equip);
                if (ctrl != null)
                {
                    Canvas.SetLeft(ctrl, equip.Position.X);
                    Canvas.SetTop(ctrl, equip.Position.Y);
                    RuntimeCanvas.Children.Add(ctrl);
                    _elementMap[equip.Id] = ctrl;
                }
            }

            // 创建管道
            foreach (var pipe in _project.Pipes)
            {
                var pipeCtrl = new PipeControl { PipeModel = pipe };
                RuntimeCanvas.Children.Add(pipeCtrl);
                _pipeMap[pipe.Id] = pipeCtrl;
            }

            // 启动动画
            foreach (var pipeCtrl in _pipeMap.Values)
            {
                pipeCtrl.StartAnimation();
            }
        }

        private void InitializeTagSimulator()
        {
            _simulatedTagValues.Clear();

            // 从所有设备收集标签绑定
            foreach (var equip in _project.Equipments)
            {
                if (equip.TagBindings != null)
                {
                    foreach (var binding in equip.TagBindings)
                    {
                        if (!_simulatedTagValues.ContainsKey(binding.Value))
                        {
                            _simulatedTagValues[binding.Value] = GenerateInitialTagValue(binding.Key);
                        }
                    }
                }
            }
        }

        private object GenerateInitialTagValue(string propertyName)
        {
            return propertyName switch
            {
                "State" => EquipmentState.Stopped,
                "Position.X" => 0.0,
                "Position.Y" => 0.0,
                "Width" => 100.0,
                "Height" => 100.0,
                "Rotation" => 0.0,
                "RotorSpeed" => 0.0,
                "MotorState" => MotorState.Stopped,
                "ImpellerCount" => 2,
                "DoorState" => DoorState.Closed,
                "FillRatio" => 0.0,
                "DustLevel" => 0.0,
                "Temperature" => 20.0,
                "Pressure" => 0.0,
                "Speed" => 0.0,
                "Power" => 0.0,
                "Energy" => 0.0,
                "Weight" => 0.0,
                "BeltSpeed" => 0.0,
                "FlowDirection" => TwoD5DMonitoring.Models.FlowDirection.Forward,
                "ValveState" => ValveState.Closed,
                "ValveAngle" => 0.0,
                "BlowAngle" => 0.0,
                "ReducerType" => ReducerType.Concentric,
                "InletDiameter" => 100.0,
                "OutletDiameter" => 50.0,
                "Length" => 200.0,
                "MainDiameter" => 100.0,
                "BranchDiameter" => 50.0,
                "BranchAngle" => 45.0,
                _ => 0.0
            };
        }

        private void OnTagSimulationTick(object sender, EventArgs e)
        {
            // 模拟标签值变化
            foreach (var key in _simulatedTagValues.Keys.ToList())
            {
                var currentValue = _simulatedTagValues[key];
                var newValue = SimulateTagValueChange(key, currentValue);
                _simulatedTagValues[key] = newValue;
            }

            // 更新设备属性
            ApplyTagValuesToEquipment();
        }

        private object SimulateTagValueChange(string propertyName, object currentValue)
        {
            return propertyName switch
            {
                "State" => _random.NextDouble() > 0.95 ? (object)EquipmentState.Running : EquipmentState.Stopped,
                "RotorSpeed" => Math.Max(0, (double)currentValue + _random.Next(-50, 50)),
                "MotorState" => _random.NextDouble() > 0.9 ? (object)MotorState.Running : MotorState.Stopped,
                "DoorState" => _random.NextDouble() > 0.95 ? (object)DoorState.Open : DoorState.Closed,
                "FillRatio" => Math.Clamp((double)currentValue + _random.Next(-2, 3), 0, 100),
                "DustLevel" => Math.Clamp((double)currentValue + _random.Next(-1, 2), 0, 100),
                "Temperature" => Math.Clamp((double)currentValue + _random.Next(-2, 3), 15, 80),
                "Pressure" => Math.Clamp((double)currentValue + _random.NextDouble() * 0.1 - 0.05, 0, 2.0),
                "Speed" => Math.Max(0, (double)currentValue + _random.Next(-10, 10)),
                "Power" => Math.Max(0, (double)currentValue + _random.Next(-5, 5)),
                "Energy" => (double)currentValue + _random.NextDouble() * 0.5,
                "Weight" => Math.Max(0, (double)currentValue + _random.Next(-10, 10)),
                "BeltSpeed" => Math.Max(0, (double)currentValue + _random.Next(-5, 5)),
                "ValveState" => _random.NextDouble() > 0.9 ? (object)ValveState.Open : ValveState.Closed,
                "ValveAngle" => Math.Clamp((double)currentValue + _random.Next(-10, 10), 0, 90),
                "BranchAngle" => Math.Clamp((double)currentValue + _random.Next(-5, 5), 0, 90),
                _ => currentValue
            };
        }

        private void ApplyTagValuesToEquipment()
        {
            foreach (var equip in _project.Equipments)
            {
                if (equip.TagBindings == null) continue;

                foreach (var binding in equip.TagBindings)
                {
                    if (_simulatedTagValues.TryGetValue(binding.Value, out var value))
                    {
                        ApplyTagValueToModel(equip, binding.Key, value);
                    }
                }
            }
        }

        private void ApplyTagValueToModel(EquipmentBase model, string propertyName, object value)
        {
            try
            {
                var prop = model.GetType().GetProperty(propertyName);
                if (prop != null && prop.CanWrite)
                {
                    var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(model, convertedValue);
                }
            }
            catch
            {
                // Ignore conversion errors
            }
        }

        private BaseEquipmentControl CreateRuntimeControl(EquipmentBase model)
        {
            return model.Type switch
            {
                EquipmentType.RootBlower => new BlowerControl { EquipmentModel = model },
                EquipmentType.DustCollector => new DustCollectorControl { EquipmentModel = model },
                EquipmentType.FeedBin => new FeedBinControl { EquipmentModel = model },
                EquipmentType.PressurizedTank => new PressurizedTankControl { EquipmentModel = model },
                EquipmentType.CarbonBlackTank => new TankControl { EquipmentModel = model, TankType = Models.TankType.CarbonBlack },
                EquipmentType.OilTank => new TankControl { EquipmentModel = model, TankType = Models.TankType.Oil },
                EquipmentType.CarbonBlackScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.CarbonBlack },
                EquipmentType.OilScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.Oil },
                EquipmentType.Mixer => new MixerControl { EquipmentModel = model },
                EquipmentType.RubberScale => new ScaleControl { EquipmentModel = model, ScaleType = Models.ScaleType.Rubber },
                EquipmentType.RubberConveyor => new ConveyorControl { EquipmentModel = model },
                EquipmentType.OilPump => new OilPumpControl { EquipmentModel = model },
                EquipmentType.SideBlowValve => new SideBlowValveControl { EquipmentModel = model },
                EquipmentType.ThreeWayValve => new ValveControl { EquipmentModel = model, ValveType = Models.ValveType.YType },
                EquipmentType.TwoWayValve => new ValveControl { EquipmentModel = model, ValveType = Models.ValveType.VType },
                EquipmentType.PipeReducer => new PipeReducerControl { EquipmentModel = model },
                EquipmentType.SideBlowDevice => new SideBlowDeviceControl { EquipmentModel = model },
                _ => null
            };
        }

        private void OnUpdateTimer(object sender, EventArgs e)
        {
            // Update equipment state
            foreach (var ctrl in _elementMap.Values)
            {
                ctrl.UpdateVisualState();
            }

            // Update pipe animation
            foreach (var pipeCtrl in _pipeMap.Values)
            {
                pipeCtrl.UpdateFlowAnimationSpeed();
            }
        }

        private void OnStartAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var ctrl in _elementMap.Values)
            {
                if (ctrl.EquipmentModel != null)
                    ctrl.EquipmentModel.State = EquipmentState.Running;
            }
            UpdateStatusText("系统已启动");
        }

        private void OnStopAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var ctrl in _elementMap.Values)
            {
                if (ctrl.EquipmentModel != null)
                    ctrl.EquipmentModel.State = EquipmentState.Stopped;
            }
            UpdateStatusText("系统已停止");
        }

        private void OnEmergencyStopClick(object sender, RoutedEventArgs e)
        {
            foreach (var ctrl in _elementMap.Values)
            {
                if (ctrl.EquipmentModel != null)
                    ctrl.EquipmentModel.State = EquipmentState.Alarm;
            }
            UpdateStatusText("紧急停止");
        }

        private void OnSwitchToDesignerClick(object sender, RoutedEventArgs e)
        {
            // 切换到设计器模式
        }

        private void UpdateStatusText(string text)
        {
            if (StatusText != null)
                StatusText.Text = text;
        }

        public void Stop()
        {
            _updateTimer?.Stop();

            foreach (var pipeCtrl in _pipeMap.Values)
            {
                pipeCtrl.StopAnimation();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}