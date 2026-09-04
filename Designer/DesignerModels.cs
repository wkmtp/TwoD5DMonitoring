using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Designer
{
    public class ToolboxItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public EquipmentType Type { get; set; }
        public string Description { get; set; }
        public object Icon { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class TreeNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        public object Tag { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}