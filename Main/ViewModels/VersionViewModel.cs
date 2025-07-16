using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class VersionViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string version;
        [ObservableProperty]
        private string mcuVersion;

        public VersionViewModel()
        {
            UpdateVersionInfo();
        }

        protected override void Broadcast<T>(T oldValue, T newValue, string propertyName)
        {
            base.Broadcast(oldValue, newValue, propertyName);
          
        }

           
        [RelayCommand]
        private void UpdateVersionCommand()
        {
            UpdateVersionInfo();
        }
        
        // 添加公共方法用于从View中调用
        public void RefreshVersionInfo()
        {
            UpdateVersionInfo();
        }
        
        private void UpdateVersionInfo()
        {
            Version = $"上位机版本:{System.Windows.Application.ResourceAssembly.GetName().Version.ToString()}";
            McuVersion = $"MCU版本:{SystemGlobal.McuVersion}"; 
        }
    }
}
