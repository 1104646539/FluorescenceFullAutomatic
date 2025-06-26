using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls.Dialogs;
using Serilog;

namespace FluorescenceFullAutomatic.Utils
{
    public class GlobalUtil
    {  
      
        public static string GetCurrentProjectPath
        {

            get
            {
                //return Environment.CurrentDirectory.Replace(@"\bin\Debug\net6.0-windows", "");//获取具体路径
                return Environment.CurrentDirectory;//获取具体路径
            }
        }
        /// <summary>
        /// 是否是双联质控卡
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns></returns>
        public static bool IsDoubleQCCard(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode) || qrCode.Length != 51)
            {
                return false;
            }
            String project = qrCode.Substring(0, 3);
            if (SqlHelper.CODE_DQC == project)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否是双联检测卡
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns></returns>
        public static bool IsDoubleCard(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode) || qrCode.Length != 51)
            {
                return false;
            }
            String project = qrCode.Substring(0, 3);
            for (int i = 0; i < SqlHelper.codes2.Length; i++)
            {
                if (SqlHelper.codes2[i] == project)
                {
                    return true;
                }
            }
            return false;
        }

        public static string ToStringOrNull(string str, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            return str;
        }

        /// <summary>
        /// 是否是单联质控卡
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns></returns>
        public static bool IsSingleQCCard(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                return false;
            }
            String project = qrCode.Split(',')[0];
            if (project == SqlHelper.CODE_QC)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否是单联检测卡
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns></returns>
        public static bool IsSingleCard(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                return false;
            }
            String[] items = qrCode.Split(',');
            if (items.Length == 7)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取资源字典中的字符串值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetString(string key)
        {
            return Keys.GetString(key);
        }

        /// <summary>
        /// 获取升级盘符
        /// </summary>
        /// <returns></returns>
        public static string GetUpdateFlash()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (
                    d.DriveType == DriveType.Removable
                    && d.VolumeLabel == SystemGlobal.UpdateFlashName
                )
                {
                    Log.Information($"Found removable drive: {d.Name} {d.VolumeLabel}");
                    return d.Name;
                }
            }
            return "";
        }

        public static void ShowHiltDialog(
            string title,
            string msg,
            string confirmText,
            Action<HiltDialogViewModel, CustomDialog> actionConfirm,
            string cancelText = null,
            Action<HiltDialogViewModel, CustomDialog> actionCancel = null,
            string closeText = null,
            Action<HiltDialogViewModel, CustomDialog> actionClose = null,
            bool autoCloseDialog = true
        )
        {
            CustomDialog customDialog = new CustomDialog();
            HiltDialogViewModel hiltDialogVM = new HiltDialogViewModel(
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        MainWindow.Instance.HideMetroDialogAsync(customDialog);
                    }
                    actionConfirm?.Invoke(d, customDialog);
                },
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        MainWindow.Instance.HideMetroDialogAsync(customDialog);
                    }
                    actionCancel?.Invoke(d, customDialog);
                },
                (d) =>
                {
                    if (autoCloseDialog)
                    {
                        MainWindow.Instance.HideMetroDialogAsync(customDialog);
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
            customDialog.Content = new HiltDialog() { DataContext = hiltDialogVM };

            MainWindow.Instance.ShowMetroDialogAsync(customDialog);
        }
    }
}
