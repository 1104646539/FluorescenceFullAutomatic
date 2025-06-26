using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Config;
using System;
using System.Collections.Generic;
using System.Linq;
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
        //[ObservableProperty]
        //[NotifyPropertyChangedRecipients]
        //private bool isVisible;

        //[ObservableProperty]
        //[NotifyPropertyChangedRecipients]
        //private Visibility viewVisibility = Visibility.Collapsed;

        public VersionViewModel()
        {
            UpdateVersionInfo();
        }

        protected override void Broadcast<T>(T oldValue, T newValue, string propertyName)
        {
            base.Broadcast(oldValue, newValue, propertyName);
            //if (propertyName == nameof(IsVisible) || propertyName == nameof(ViewVisibility)) {
            //    UpdateVersionInfo();
            //}
        }

    

        //partial void OnIsVisibleChanged(bool value)
        //{
        //    // Update ViewVisibility when IsVisible changes
        //    ViewVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            
        //    // 当视图变为可见时更新版本信息
        //    if (value)
        //    {
        //        UpdateVersionInfo();
        //    }
        //}

           
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
            // 这里可以添加其他需要动态更新的版本信息
        }
    }
}
