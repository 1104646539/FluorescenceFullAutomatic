using FluorescenceFullAutomatic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FluorescenceFullAutomatic.Views
{
    /// <summary>
    /// VersionView.xaml 的交互逻辑
    /// </summary>
    public partial class VersionView : UserControl
    {
        public VersionView()
        {
            InitializeComponent();
            // 注意：InitializeComponent 由 WPF 自动生成的代码调用
            // 我们只需要添加额外的事件处理
            this.Loaded += VersionView_Loaded;
            this.IsVisibleChanged += VersionView_IsVisibleChanged;
        }

        private void VersionView_Loaded(object sender, RoutedEventArgs e)
        {
            // 控件加载完成时的处理
        }

        private void VersionView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is VersionViewModel viewModel)
            {
                bool isVisible = (bool)e.NewValue;
                //viewModel.IsVisible = isVisible;
                
                // 当视图变为可见时更新版本信息
                if (isVisible)
                {
                    viewModel.RefreshVersionInfo();
                }
            }
        }
    }
}
