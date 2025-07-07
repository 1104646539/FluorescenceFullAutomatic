using FluorescenceFullAutomatic.Repositorys;
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
    public class FakeDialogRepository : IDialogRepository
    {
        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return Task.FromResult(true);
        }

        public void ShowHiltDialog(string title, string msg, string confirmText, Action<HiltDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HiltDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HiltDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            
        }

        public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return null;
        }
    }
}
