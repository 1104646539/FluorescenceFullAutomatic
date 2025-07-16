using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.Platform.ViewModels;
using FluorescenceFullAutomatic.Platform.Views;
using FluorescenceFullAutomatic.Platform.Views.Ctr;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IDialogService
    {
        
        public void ShowHiltDialog(object context,
            string title,
            string msg,
            string confirmText,
            Action<HintDialogViewModel, CustomDialog> actionConfirm,
            string cancelText = null,
            Action<HintDialogViewModel, CustomDialog> actionCancel = null,
            string closeText = null,
            Action<HintDialogViewModel, CustomDialog> actionClose = null,
            bool autoCloseDialog = true
        );

        public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings settings = null);

        Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null);

    }

    public class DialogService : IDialogService
    {
        IDialogCoordinator dialogCoordinator;
        public DialogService(IDialogCoordinator dialogCoordinator) { 
            this.dialogCoordinator = dialogCoordinator;
        }
        public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return dialogCoordinator.HideMetroDialogAsync(context,dialog,settings);
            //return window.HideMetroDialogAsync(dialog, settings);
        }

        public void ShowHiltDialog(object context, string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm,
            string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            CustomDialog customDialog = new CustomDialog();
            HintDialogViewModel hiltDialogVM = new HintDialogViewModel(
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        dialogCoordinator.HideMetroDialogAsync(context,customDialog);
                    }
                    actionConfirm?.Invoke(d, customDialog);
                },
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        dialogCoordinator.HideMetroDialogAsync(context, customDialog);
                    }
                    actionCancel?.Invoke(d, customDialog);
                },
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        dialogCoordinator.HideMetroDialogAsync(context, customDialog);
                    }
                    actionClose?.Invoke(d, customDialog);
                }
            )
            {
                Title = title,
                Msg = msg,
                ConfirmText = confirmText,
                CancelText = cancelText,
                CloseText = closeText,
            };
            customDialog.Content = new HintDialog() { DataContext = hiltDialogVM };

            dialogCoordinator.ShowMetroDialogAsync(context,customDialog);
            //GlobalUtil.ShowHiltDialog(context, title, msg,confirmText,actionConfirm,cancelText,actionCancel,closeText,actionClose,autoCloseDialog);
        }

        public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return dialogCoordinator.ShowProgressAsync(context, title, message, isCancelable, settings);
            //return window.ShowProgressAsync(title,message,isCancelable,settings);
        }
    }
}
