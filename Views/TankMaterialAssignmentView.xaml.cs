using System.Windows;
using System.Windows.Controls;
using TwoD5DMonitoring.ViewModels;

namespace TwoD5DMonitoring.Views
{
    public partial class TankMaterialAssignmentView : UserControl
    {
        public TankMaterialAssignmentView()
        {
            InitializeComponent();
        }

        private void TankSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 实现搜索过滤逻辑
            // TODO: 绑定到 ViewModel 的搜索属性
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TankMaterialAssignmentViewModel vm)
            {
                _ = vm.LoadAsync();
            }
        }

        private void AssignMaterial_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 打开分配物料对话框
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 切换到历史标签页
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 导出报表
        }

        private void SaveAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TankMaterialAssignmentViewModel vm)
            {
                _ = vm.SaveAssignmentAsync();
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            // 隐藏编辑面板
            EditPanel.Visibility = Visibility.Collapsed;
        }
    }
}