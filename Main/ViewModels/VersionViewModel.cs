using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Utils;
using MahApps.Metro.Controls.Dialogs;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class VersionViewModel : ObservableRecipient, IReceiveData
    {
        [ObservableProperty]
        private string version;

        [ObservableProperty]
        private string mcuVersion;
        private readonly ISerialPortService serialPortService;
        private readonly ISerialPortCommandFacade serialPortCommandFacade;
        private readonly IDialogService dialogService;
        private readonly ILogService logService;
        private readonly IDispatcherService dispatcherService;

        public VersionViewModel(
            ISerialPortService serialPortService,
            ISerialPortCommandFacade serialPortCommandFacade,
            IDialogService dialogService,
            ILogService logService,
            IDispatcherService dispatcherService
        )
        {
            this.dispatcherService = dispatcherService;
            this.serialPortService = serialPortService;
            this.serialPortCommandFacade = serialPortCommandFacade;
            this.dialogService = dialogService;
            this.logService = logService;
            UpdateVersionInfo();
            // this.serialPortService.AddReceiveData(this);
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
            Version =
                $"上位机版本:{System.Windows.Application.ResourceAssembly.GetName().Version.ToString()}";
            McuVersion = $"MCU版本:{SystemGlobal.McuVersion}";
        }

        [RelayCommand]
        private void MCUUpdate()
        {
            dialogService.ShowHiltDialog(
                this,
                "提示",
                "确定需要更新MCU吗？请先插入升级U盘。",
                "确定升级",
                (vm, d) =>
                {
                    McuUpdateClickStart();
                },
                "取消",
                (vm, d) => { }
            );
        }

        /// <summary>
        /// 开始升级MCU
        /// 1、检查升级U盘是否存在，升级文件是否存在，只能插入一个U盘
        /// 2、打开下位机升级功能，打开下位机升级盘
        /// 3、复制U盘升级文件到升级盘
        /// 4、重启仪器，升级(可选)
        /// </summary>
        private void McuUpdateClickStart()
        {
            //Step 1
            VerifyUpdate();
        }

        /// <summary>
        /// u盘目录
        /// </summary>
        string upanDrive = "";

        /// <summary>
        /// Step 1、验证升级条件
        /// </summary>
        private void VerifyUpdate()
        {
            Collection<DriveInfo> drives = GlobalUtil.GetRemovebleDrives();

            string updateDrive = GlobalUtil.GetUpdateFlash();
            if (drives.Count == 0 || (drives.Count == 1 && !string.IsNullOrEmpty(updateDrive)))
            {
                //检查到没有U盘,或者有一个u盘，但是是升级盘
                UpdateFailed("未检查到U盘，请检查是否插入U盘或重新插入");
                return;
            }
            else if (
                (string.IsNullOrEmpty(updateDrive) && drives.Count == 1)
                || (!string.IsNullOrEmpty(updateDrive) && drives.Count == 2)
            )
            {
                //(检查到2个U盘，但有一个是升级盘)或者(检查到一个U盘，没有升级盘)，正常
                //Step 2、打开升级盘
                upanDrive = drives
                    .FirstOrDefault((d) => !d.VolumeLabel.Equals(SystemGlobal.UpdateFlashName))
                    ?.Name;

                OpenUpdateDrive();
            }
            else
            {
                //检查到多个U盘，提示错误
                UpdateFailed("检测到多个U盘，请只插入一个U盘");
                return;
            }
        }

        ProgressDialogController progressController;

        /// <summary>
        /// Step 2、打开升级盘
        /// </summary>
        private async void OpenUpdateDrive()
        {
             Task.Run(async () =>
            {
                progressController = await dialogService.ShowProgressAsync(
                    this,
                    "提示",
                    "正在升级，请等待."
                );
                progressController.SetIndeterminate();
            });
           var ret =  serialPortCommandFacade.UpdateAsync();
           ReceiveUpdateModel(ret.Result);
            // Task.Run(async () =>
            // {
            //     progressController = await dialogService.ShowProgressAsync(
            //         this,
            //         "提示",
            //         "正在升级，请等待."
            //     );
            //     progressController.SetIndeterminate();
            // });
        }

        /// <summary>
        /// 接收到 打开升级盘成功
        /// </summary>
        /// <param name="model"></param>
        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            logService.Info($"接收到 打开升级盘 {model}");
            if (SystemGlobal.TestType == TestType.Debug)
                return;

            if ("1".Equals(model.Data.Ready))
            {
                //打开升级盘成功
                //Step 3、复制U盘升级文件到升级盘
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    CopyUpdateFileToUpdateDrive();
                });
            }
            else
            {
                //打开失败
                UpdateFailed("打开升级盘符失败");
            }
        }

        /// <summary>
        /// 升级失败
        /// </summary>
        /// <param name="v"></param>
        private async void UpdateFailed(string err)
        {
            if (progressController != null)
            {
                await progressController.CloseAsync();
            }
            dispatcherService.Invoke(() =>
            {
                
                dialogService.ShowHiltDialog(
                    this,
                    "提示",
                    $"升级失败,{err}",
                    "确定",
                    (vm, d) => { },
                    null,
                    null
                );
            });
        }

        /// <summary>
        /// 升级成功
        /// </summary>
        /// <param name="v"></param>
        private async void UpdateSuccessed()
        {
            if (progressController != null)
            {
                await progressController.CloseAsync();
            }
            dispatcherService.Invoke(() =>
            {
                dialogService.ShowHiltDialog(
                    this,
                    "提示",
                    $"升级成功，请重启仪器完成升级。",
                    "确定",
                    (vm, d) => { },
                    null,
                    null
                );
            });
        }

        /// <summary>
        /// Step 3、复制U盘升级文件到升级盘
        /// </summary>
        private void CopyUpdateFileToUpdateDrive()
        {
            string target = GlobalUtil.GetUpdateFlash();
            if (string.IsNullOrEmpty(target))
            {
                UpdateFailed("未找到升级盘");
                return;
            }
            string ret = GlobalUtil.CopyFileToTarget(
                Path.Combine(upanDrive, SystemGlobal.UpdateFileName),
                target
            );

            if (string.IsNullOrEmpty(ret))
            {
                //升级成功
                UpdateSuccessed();
            }
            else
            {
                //升级失败
                UpdateFailed(ret);
            }
        }

        public void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model) { }

        public void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model) { }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model) { }

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model) { }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model) { }

        public void ReceiveCleanoutSamplingProbeModel(
            BaseResponseModel<CleanoutSamplingProbeModel> model
        ) { }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model) { }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model) { }

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model) { }

        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model) { }

        public void ReceiveTestModel(BaseResponseModel<TestModel> model) { }

        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model) { }

        public void ReceiveClearReactionAreaModel(
            BaseResponseModel<ClearReactionAreaModel> model
        ) { }

        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model) { }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model) { }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model) { }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model) { }

        public void ReceiveVersionModel(BaseResponseModel<VersionModel> model) { }

        public void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model) { }

        public void ReceiveStateError(BaseResponseModel<dynamic> model) { }
    }
}
