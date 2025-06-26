using MahApps.Metro.Controls;
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
using FluorescenceFullAutomatic.ViewModels;
using Serilog;
using FluorescenceFullAutomatic.Views.Ctr;
using FluorescenceFullAutomatic.Model;
using System.Windows.Controls.Primitives;

namespace FluorescenceFullAutomatic.Views
{
    /// <summary>
    /// DataManagerControl.xaml 的交互逻辑
    /// </summary>
    public partial class DataManagerView : MetroContentControl
    {
        
        public DataManagerView()
        {
            InitializeComponent();
            
        }

        private void MetroContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is DataManagerViewModel viewModel)
            {
                Log.Information($"MetroContentControl_SizeChanged {e.NewSize.Width} {e.NewSize.Height}");
                viewModel.ControlWidth = e.NewSize.Width;
                viewModel.ControlHeight = e.NewSize.Height;
            }
        }

   
    }
}
