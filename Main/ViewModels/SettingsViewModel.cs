using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private int selectedTabIndex = 0;

        
        public SettingsViewModel()
        {
        }

        [RelayCommand]
        public void ClickDebug() 
        {
            Log.Information("ClickDebug 命令执行");
            DebugView debugView = new DebugView();
            debugView.ShowDialog();
        }

        [RelayCommand]
        public void SelectTab(int index)
        {
            Log.Information($"SelectTab 命令执行，选择索引: {index}");
            SelectedTabIndex = index;
        }
    }
}
