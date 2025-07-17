using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.ViewModels;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakeDialogRepository : IDialogService
    {
        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return Task.FromResult(true);
        }

        public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return Task.FromResult(true);
        }

        public void ShowHiltDialog(string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            
        }

        public void ShowHiltDialog(object context, string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            
        }

        public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return null;
        }
    }
}
