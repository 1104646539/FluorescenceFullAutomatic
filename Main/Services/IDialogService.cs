using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NPOI.SS.Formula.Functions;

namespace FluorescenceFullAutomatic.Services
{
    public interface IDialogService
    {
        
        public void ShowHiltDialog(
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

        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null);

        Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null);

    }

    public class DialogRepository : IDialogService
    {
        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return window.HideMetroDialogAsync(dialog, settings);
        }

        public void ShowHiltDialog(string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
             GlobalUtil.ShowHiltDialog(title,msg,confirmText,actionConfirm,cancelText,actionCancel,closeText,actionClose,autoCloseDialog);
        }

        public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return MainWindow.Instance.ShowProgressAsync(title,message,isCancelable,settings);
        }
    }
}
