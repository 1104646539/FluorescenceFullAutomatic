using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private int selectedTabIndex = 0;

        [RelayCommand]
        public void ClickDebug() {
            DebugView debugView = new DebugView();
            debugView.ShowDialog();
        }

        [RelayCommand]
        public void SelectTab(int index)
        {
            SelectedTabIndex = index;
        }
    
    }
}
