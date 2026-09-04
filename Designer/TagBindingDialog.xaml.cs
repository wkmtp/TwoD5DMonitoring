using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Designer
{
    public partial class TagBindingDialog : Window, INotifyPropertyChanged
    {
        private Dictionary<string, string> _tagBindings;
        private EquipmentBase _equipmentModel;

        public TagBindingDialog(Dictionary<string, string> tagBindings, EquipmentBase equipmentModel = null)
        {
            InitializeComponent();
            _tagBindings = tagBindings ?? new Dictionary<string, string>();
            _equipmentModel = equipmentModel;
            DataContext = this;
            
            // Initialize available properties based on equipment type
            InitializeAvailableProperties();
            
            // Load existing tag bindings
            foreach (var kvp in _tagBindings)
            {
                TagBindings.Add(new TagBindingItem { Key = kvp.Key, Value = kvp.Value });
            }
        }

        public ObservableCollection<TagBindingItem> TagBindings { get; } = new();

        public List<string> AvailableProperties { get; private set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void InitializeAvailableProperties()
        {
            AvailableProperties.Clear();
            
            if (_equipmentModel != null)
            {
                // Common properties for all equipment
                AvailableProperties.AddRange(new[] { "State", "Position.X", "Position.Y", "Width", "Height", "Rotation" });
                
                // Equipment-specific properties
                switch (_equipmentModel.Type)
                {
                    case EquipmentType.RootBlower:
                        AvailableProperties.AddRange(new[] { "RotorSpeed", "MotorState", "ImpellerCount" });
                        break;
                    case EquipmentType.DustCollector:
                        AvailableProperties.Add("MotorState");
                        break;
                    case EquipmentType.FeedBin:
                        AvailableProperties.AddRange(new[] { "DoorState", "FillRatio", "DustLevel" });
                        break;
                    case EquipmentType.PressurizedTank:
                        AvailableProperties.AddRange(new[] { "FillRatio", "InletValveOpen", "OutletValveOpen", "HighLevelState", "LowLevelState" });
                        break;
                    case EquipmentType.CarbonBlackTank:
                    case EquipmentType.OilTank:
                        AvailableProperties.AddRange(new[] { "FillRatio", "Temperature" });
                        break;
                    case EquipmentType.Mixer:
                        AvailableProperties.AddRange(new[] { "Temperature", "Pressure", "Speed", "Power", "Energy", "FeedDoorState", "DischargeDoorState", "RotorAnimationSpeed" });
                        break;
                    case EquipmentType.CarbonBlackScale:
                    case EquipmentType.OilScale:
                    case EquipmentType.RubberScale:
                        AvailableProperties.AddRange(new[] { "Weight", "FillRatio" });
                        break;
                    case EquipmentType.RubberConveyor:
                        AvailableProperties.AddRange(new[] { "BeltSpeed", "MotorState", "HasRubberBlocks" });
                        break;
                    case EquipmentType.OilPump:
                        AvailableProperties.AddRange(new[] { "RotorSpeed", "MotorState", "FlowDirection" });
                        break;
                    case EquipmentType.SideBlowValve:
                        AvailableProperties.AddRange(new[] { "ValveAngle", "ValveState", "BlowAngle" });
                        break;
                    case EquipmentType.ThreeWayValve:
                    case EquipmentType.TwoWayValve:
                        AvailableProperties.AddRange(new[] { "ValveState", "FlowDirection" });
                        break;
                    case EquipmentType.PipeReducer:
                        AvailableProperties.AddRange(new[] { "ReducerType", "InletDiameter", "OutletDiameter", "Length" });
                        break;
                    case EquipmentType.SideBlowDevice:
                        AvailableProperties.AddRange(new[] { "MainDiameter", "BranchDiameter", "BranchAngle", "ValveState" });
                        break;
                }
            }
            else
            {
                // Generic properties when no equipment model provided
                AvailableProperties.AddRange(new[] 
                { 
                    "State", "Position.X", "Position.Y", "Width", "Height", "Rotation",
                    "RotorSpeed", "MotorState", "ImpellerCount",
                    "DoorState", "FillRatio", "DustLevel",
                    "Temperature", "Pressure", "Speed", "Power", "Energy",
                    "Weight", "BeltSpeed", "FlowDirection",
                    "ValveState", "ValveAngle", "BlowAngle",
                    "ReducerType", "InletDiameter", "OutletDiameter", "Length",
                    "MainDiameter", "BranchDiameter", "BranchAngle", "ValveState"
                });
            }
            
            OnPropertyChanged(nameof(AvailableProperties));
        }

        private void OnAddRowClick(object sender, RoutedEventArgs e)
        {
            TagBindings.Add(new TagBindingItem { Key = "", Value = "" });
        }

        private void OnDeleteRowClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                var item = TagBindings.FirstOrDefault(x => x.Key == key);
                if (item != null)
                    TagBindings.Remove(item);
            }
        }

        private void OnPreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Ensure the ComboBox gets focus
        }

        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.Item as TagBindingItem;
                if (item != null && string.IsNullOrWhiteSpace(item.Key))
                {
                    // Remove empty rows
                    e.Cancel = true;
                    TagBindings.Remove(item);
                }
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            // Validate and collect tag bindings
            var result = new Dictionary<string, string>();
            foreach (var item in TagBindings)
            {
                if (!string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                {
                    if (result.ContainsKey(item.Key))
                    {
                        MessageBox.Show($"重复的属性名: {item.Key}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    result[item.Key] = item.Value;
                }
            }
            
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class TagBindingItem : INotifyPropertyChanged
    {
        private string _key;
        private string _value;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}