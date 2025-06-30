using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NPOI.SS.Formula.Functions;

namespace FluorescenceFullAutomatic.Repositorys
{
    public interface IDialogRepository
    {
        
        public void ShowHiltDialog(
            string title,
            string msg,
            string confirmText,
            Action<HiltDialogViewModel, CustomDialog> actionConfirm,
            string cancelText = null,
            Action<HiltDialogViewModel, CustomDialog> actionCancel = null,
            string closeText = null,
            Action<HiltDialogViewModel, CustomDialog> actionClose = null,
            bool autoCloseDialog = true
        );

        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null);
    }

    public class DialogRepository : IDialogRepository
    {
        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return window.HideMetroDialogAsync(dialog, settings);
        }

        public void ShowHiltDialog(string title, string msg, string confirmText, Action<HiltDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HiltDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HiltDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
             GlobalUtil.ShowHiltDialog(title,msg,confirmText,actionConfirm,cancelText,actionCancel,closeText,actionClose,autoCloseDialog);
        }
    }
}
