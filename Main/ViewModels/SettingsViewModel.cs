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
using System.Windows;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private int selectedTabIndex = 0;
        private readonly IConfigService configService;

        [ObservableProperty]
        private Visibility showDebugView = Visibility.Collapsed;



        public SettingsViewModel(IConfigService configService)
        {
            this.configService = configService;
            this.configService.AddDebugModeChangedListener(OnDebugModeChanged);
            OnDebugModeChanged(this.configService.GetDebugMode());
        }
        

        private void OnDebugModeChanged(bool debugMode)
        {
            
            ShowDebugView = debugMode ? Visibility.Visible : Visibility.Collapsed;
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
