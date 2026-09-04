using System.Windows;
using System.Windows.Controls;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.ViewModels;

namespace TwoD5DMonitoring.Views
{
    public partial class MaterialManagementView : UserControl
    {
        public MaterialManagementView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MaterialManagementViewModel vm && e.NewValue is MaterialCategory category)
            {
                vm.SelectedCategory = category;
            }
        }
    }
}