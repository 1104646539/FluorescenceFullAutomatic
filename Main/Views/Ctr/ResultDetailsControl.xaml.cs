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


namespace FluorescenceFullAutomatic.Views.Ctr
{
    /// <summary>
    /// ResultDetailsControl.xaml 的交互逻辑
    /// </summary>
    public partial class ResultDetailsControl : MahApps.Metro.Controls.MetroContentControl
    {
        public ResultDetailsControl()
        {
            InitializeComponent();
            
        }

        internal void Update(ResultDetailsViewModel resultDetailsViewModel)
        {
            resultDetailsViewModel.SetPlotView(this.plotView);
            this.DataContext = resultDetailsViewModel;
        }
    }
}
