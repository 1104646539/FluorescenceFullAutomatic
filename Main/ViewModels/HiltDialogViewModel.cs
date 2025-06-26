using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class HiltDialogViewModel : ObservableRecipient
    {
        [ObservableProperty]
        string title;
        [ObservableProperty]
        string msg;

        [ObservableProperty]
        string confirmText;
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        string cancelText;
        [NotifyPropertyChangedRecipients]
        [ObservableProperty]
        string closeText;

        [ObservableProperty]
        Visibility showCancel;
        [ObservableProperty]
        Visibility showClose;

        Action<HiltDialogViewModel> actionConfirm;
        Action<HiltDialogViewModel> actionCancel;
        Action<HiltDialogViewModel> actionClose;
        public HiltDialogViewModel(Action<HiltDialogViewModel> actionConfirm, Action<HiltDialogViewModel> actionCancel = null
            , Action<HiltDialogViewModel> actionClose = null)
        {
            this.Title = "";
            this.actionConfirm = actionConfirm;
            this.actionCancel = actionCancel;
            this.actionClose = actionClose;

            ShowCancel = string.IsNullOrEmpty(CancelText) ? Visibility.Collapsed : Visibility.Visible;
            ShowClose = string.IsNullOrEmpty(CloseText) ? Visibility.Collapsed : Visibility.Visible;

        }
        protected override void Broadcast<T>(T oldValue, T newValue, string? propertyName)
        {
            base.Broadcast(oldValue, newValue, propertyName);

            if (propertyName == nameof(CancelText))
            {
                ShowCancel = string.IsNullOrEmpty(CancelText) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (propertyName == nameof(CloseText))
            {
                ShowClose = string.IsNullOrEmpty(CloseText) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        [RelayCommand]
        public void Confirm()
        {
            actionConfirm?.Invoke(this);
        }
        [RelayCommand]
        public void Cancel()
        {
            actionCancel?.Invoke(this);
        }
        [RelayCommand]
        public void Close()
        {
            actionClose?.Invoke(this);
        }
    }
}
